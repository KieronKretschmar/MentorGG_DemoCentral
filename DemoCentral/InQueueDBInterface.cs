using System;
using System.Linq;
using RabbitCommunicationLib.Enums;
using DataBase.DatabaseClasses;
using Database.Enumerals;
using System.Collections.Generic;


namespace DemoCentral
{
    /// <summary>
    /// Interface for the InQueueDemo table of the database
    /// </summary>
    public interface IInQueueDBInterface
    {
        /// <summary>
        /// Add a new demo to the queue, and set all queue status to false
        /// </summary>
        void Add(long matchId, DateTime matchDate, Source source, long uploaderId);
        InQueueDemo GetDemoById(long matchId);
        /// <summary>
        /// Get a list of all<see cref="InQueueDemo"/> for a certain player
        /// </summary>
        List<InQueueDemo> GetPlayerMatchesInQueue(long playerId);
        int GetQueuePosition(InQueueDemo demo);
        int GetQueuePosition(long matchId);
        int GetTotalQueueLength();
        int IncrementRetry(InQueueDemo demo);
        int IncrementRetry(long matchId);
        void RemoveDemoFromQueue(InQueueDemo demo);
        void RemoveDemoFromQueue(long matchId);
        void RemoveDemoIfNotInAnyQueue(InQueueDemo demo);
        void RemoveDemoIfNotInAnyQueue(long matchId);
        /// <summary>
        /// Update the status for a certain queue
        /// </summary>
        /// <remarks>if all queues are set to false after execution the demo gets removed from the table</remarks>
        /// <param name="inQueue">bool if it is in that queue</param>
        void UpdateProcessStatus(long matchId, ProcessedBy process, bool processing);
        /// <summary>
        /// Update the status for a certain queue
        /// </summary>
        /// <remarks>if all queues are set to false after execution the demo gets removed from the table</remarks>
        /// <param name="inQueue">bool if it is in that queue</param>
        void UpdateProcessStatus(ProcessedBy process, bool processing, InQueueDemo demo);
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

        public void UpdateProcessStatus(long matchId, ProcessedBy process, bool processing)
        {
            InQueueDemo demo = GetDemoById(matchId);
            UpdateProcessStatus(process, processing, demo);
        }

        public void UpdateProcessStatus(ProcessedBy process, bool processing, InQueueDemo demo)
        {
            switch (process)
            {
                case ProcessedBy.DemoDownloader:
                    demo.DDQUEUE = processing;
                    break;
                case ProcessedBy.DemoFileWorker:
                    demo.DFWQUEUE = processing;
                    break;
                case ProcessedBy.SituationsOperator:
                    demo.SOQUEUE = processing;
                    break;
                default:
                    throw new InvalidOperationException("Unknown queue name");
            }

            _context.SaveChanges();
        }

        public void RemoveDemoIfNotInAnyQueue(long matchId)
        {
            var demo = GetDemoById(matchId);

            RemoveDemoIfNotInAnyQueue(demo);
        }

        public void RemoveDemoIfNotInAnyQueue(InQueueDemo demo)
        {
            if (!demo.InAnyQueue())
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

            RemoveDemoFromQueue(demo);
        }

        public void RemoveDemoFromQueue(InQueueDemo demo)
        {
            _context.InQueueDemo.Remove(demo);
            _context.SaveChanges();
        }

        public int GetQueuePosition(long matchId)
        {
            var demo = GetDemoById(matchId);
            return GetQueuePosition(demo);
        }

        public int GetQueuePosition(InQueueDemo demo)
        {

            //TODO OPTIONAL OPTIMIZATION queue position
            //Currently the same demos get checked for every method call, and their insert date gets compared
            //Maybe sort by insert date as the primary key? or add a rowindex which can be referenced ?
            return _context.InQueueDemo.Count(x => x.InsertDate < demo.InsertDate);
        }

        /// <summary>
        /// Increments the number of retries for this demo and return the new number
        /// </summary>
        /// <param name="matchId"></param>
        /// <returns></returns>
        public int IncrementRetry(long matchId)
        {
            var demo = GetDemoById(matchId);
            return IncrementRetry(demo);
        }

        /// <summary>
        /// Increments the number of retries for this demo and return the new number
        /// </summary>
        public int IncrementRetry(InQueueDemo demo)
        {
            var attempts = demo.Retries++;

            _context.SaveChanges();
            return attempts;
        }

        //TODO OPTIMIZATION make only one db call for the demo
        //Currently this method is private and every db interface method calls it seperately.
        //Make this method public and if someone needs multiple updates on the same demo, request it once 
        //and pass in the demo to every method
        public InQueueDemo GetDemoById(long matchId)
        {
            return _context.InQueueDemo.Single(x => x.MatchId == matchId);
        }
    }
}