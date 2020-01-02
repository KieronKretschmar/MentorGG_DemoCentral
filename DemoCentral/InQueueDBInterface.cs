using System;
using System.Linq;
using RabbitTransfer.Enums;
using DataBase.DatabaseClasses;
using System.Collections.Generic;


namespace DemoCentral
{
    public interface IInQueueDBInterface
    {
        void Add(long matchId, DateTime matchDate, Source source, long uploaderID);
        List<InQueueDemo> GetPlayerMatchesInQueue(long playerId);
        int GetQueuePosition(long matchId);
        int GetTotalQueueLength();
        int IncrementRetry(long matchId);
        void RemoveDemoFromQueue(long matchId);
        void UpdateQueueStatus(long matchId, string QueueName, bool inQueue);
    }

    public class InQueueDBInterface : IInQueueDBInterface
    {
        private readonly DemoCentralContext _context;

        public InQueueDBInterface(DemoCentralContext context)
        {
            _context = context;
        }

        public void Add(long matchId, DateTime matchDate, Source source, long uploaderId)
        {

            _context.InQueueDemo.Add(new InQueueDemo
            {
                MatchId = matchId,
                MatchDate = matchDate,
                InsertDate = DateTime.UtcNow,
                UploaderId = uploaderId,
                DFWQUEUE = false,
                SOQUEUE = false
            });

            _context.SaveChanges();

        }

        public void UpdateQueueStatus(long matchId, string QueueName, bool inQueue)
        {
            InQueueDemo demo = GetDemoById(matchId);
            //TODO make queue name enum
            switch (QueueName)
            {
                case "DFW":
                case "DemoFileWorker":
                    demo.DFWQUEUE = inQueue;
                    break;
                case "SO":
                case "SituationsOperator":
                    demo.SOQUEUE = inQueue;
                    break;
                case "DD":
                case "DemoDownloader":
                    demo.DDQUEUE = inQueue;
                    break;
                default:
                    throw new InvalidOperationException("Unknown queue");
            }


            //TODO implement better queue check, like names list etc
            List<bool> queueStates = new List<bool> { demo.DFWQUEUE, demo.SOQUEUE, demo.DDQUEUE };
            if (!queueStates.Contains(true))
            {
                _context.InQueueDemo.Remove(demo);
            }

            _context.SaveChanges();

        }

        public List<InQueueDemo> GetPlayerMatchesInQueue(long playerId)
        {
            return _context.InQueueDemo.Where(x => x.UploaderId == playerId).ToList();
        }

        public int GetTotalQueueLength()
        {
            return _context.InQueueDemo.Count();
        }

        public void RemoveDemoFromQueue(long matchId)
        {
            var demo = GetDemoById(matchId);

            _context.InQueueDemo.Remove(demo);

            _context.SaveChanges();
        }

        public int GetQueuePosition(long matchId)
        {
            var demo = GetDemoById(matchId);

            //TODO possible optimization by keeping track of row in db
            return _context.InQueueDemo.Select(x => x.InsertDate).Where(x => x < demo.InsertDate).Count();
        }

        public int IncrementRetry(long matchId)
        {
            var demo = GetDemoById(matchId);
            var attempts = demo.Retries++;

            _context.SaveChanges();
            return attempts;
        }


        private InQueueDemo GetDemoById(long matchId)
        {
            return _context.InQueueDemo.Where(x => x.MatchId == matchId).Single();
        }
    }
}