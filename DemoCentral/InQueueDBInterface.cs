using System;
using System.Linq;
using RabbitTransfer.Enums;
using DataBase.DatabaseClasses;
using Database.Enumerals;
using System.Collections.Generic;


namespace DemoCentral
{
    /// <summary>
    /// CRUD jobs for the database
    /// </summary>
    public interface IInQueueDBInterface
    {
        /// <summary>
        /// Add a new demo to the queue, and set all queue status to false
        /// </summary>
        void Add(long matchId, DateTime matchDate, Source source, long uploaderID);
        /// <summary>
        /// Get a list of all<see cref="InQueueDemo"/> for a certain player
        /// </summary>
        List<InQueueDemo> GetPlayerMatchesInQueue(long playerId);
        int GetQueuePosition(long matchId);
        int GetTotalQueueLength();
        int IncrementRetry(long matchId);
        void RemoveDemoFromQueue(long matchId);
        /// <summary>
        /// Update the status for a certain queue
        /// </summary>
        /// <remarks>if all queues are set to false after execution the demo gets removed from the table</remarks>
        /// <param name="inQueue">bool if it is in that queue</param>
        void UpdateQueueStatus(long matchId, QueueName QueueName, bool inQueue);
    }

    /// <summary>
    /// Basic implementation of the <see cref="IInQueueDBInterface"/>
    /// </summary>
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
                SOQUEUE = false,
                DDQUEUE = false,
                Retries = 0,
            });

            _context.SaveChanges();

        }

        public void UpdateQueueStatus(long matchId, QueueName QueueName, bool inQueue)
        {
            InQueueDemo demo = GetDemoById(matchId);
            switch (QueueName)
            {
                case QueueName.DemoDownloader:
                    demo.DDQUEUE = inQueue;
                    break;
                case QueueName.DemoFileWorker:
                    demo.DFWQUEUE = inQueue;
                    break;
                case QueueName.SituationsOperator:
                    demo.SOQUEUE = inQueue;
                    break;
                default:
                    throw new InvalidOperationException("Unknown queue name");
            }

            if (!demo.Queues.Contains(true))
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