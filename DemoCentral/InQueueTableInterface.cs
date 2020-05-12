using System;
using System.Linq;
using RabbitCommunicationLib.Enums;
using Database.DatabaseClasses;
using Database.Enumerals;
using System.Collections.Generic;


namespace DemoCentral
{
    /// <summary>
    /// Interface for the InQueueDemo table of the database
    /// </summary>
    public interface IInQueueTableInterface
    {
        /// <summary>
        /// Add a new demo to the queue, and set all queue status to false
        /// </summary>
        InQueueDemo Add(long matchId, DateTime matchDate, Source source, long uploaderId);
        InQueueDemo GetDemoById(long matchId);
        /// <summary>
        /// Get a list of all<see cref="InQueueDemo"/> for a certain player
        /// </summary>
        List<InQueueDemo> GetPlayerMatchesInQueue(long uploaderId);
        int GetQueuePosition(InQueueDemo demo);
        int GetTotalQueueLength();
        int IncrementRetry(InQueueDemo demo);
        void RemoveDemoFromQueue(InQueueDemo demo);
        void RemoveDemoIfNotInAnyQueue(InQueueDemo demo);
        /// <summary>
        /// Update the status for a certain queue
        /// </summary>
        /// <remarks>if all queues are set to false after execution the demo gets removed from the table</remarks>
        /// <param name="inQueue">bool if it is in that queue</param>
        void UpdateProcessStatus(InQueueDemo demo,ProcessedBy process, bool processing );
    }

    /// <summary>
    /// Basic implementation of the <see cref="IInQueueTableInterface"/>
    /// </summary>
    public class InQueueTableInterface : IInQueueTableInterface
    {
        private readonly DemoCentralContext _context;

        public InQueueTableInterface(DemoCentralContext context)
        {
            _context = context;
        }

        public InQueueDemo Add(long matchId, DateTime matchDate, Source source, long uploaderId)
        {
            var newDemo = new InQueueDemo
            {
                MatchId = matchId,
                InsertDate = DateTime.UtcNow,
                CurrentQueue =  Queue.UnQueued,
            };

            _context.InQueueDemo.Add(newDemo);

            _context.SaveChanges();

            return newDemo;
        }

        public void UpdateProcessStatus(InQueueDemo demo, ProcessedBy process, bool processing)
        {
            switch (process)
            {
                case ProcessedBy.DemoDownloader:
                    demo.CurrentQueue = Queue.DemoDownloader;
                    break;
                case ProcessedBy.DemoFileWorker:
                    demo.CurrentQueue = Queue.DemoFileWorker;
                    break;
                case ProcessedBy.SituationOperator:
                    demo.CurrentQueue = Queue.SitutationOperator;
                    break;
                default:
                    throw new InvalidOperationException("Unknown queue name");
            }

            _context.SaveChanges();
        }

        public void RemoveDemoIfNotInAnyQueue(InQueueDemo demo)
        {
            if (demo.CurrentQueue == Queue.UnQueued)
            {
                _context.InQueueDemo.Remove(demo);
            }
            _context.SaveChanges();
        }

        public List<InQueueDemo> GetPlayerMatchesInQueue(long uploaderId)
        {
            return _context.InQueueDemo.Where(x => x.Demo.UploaderId == uploaderId).ToList();
        }

        public int GetTotalQueueLength()
        {
            return _context.InQueueDemo.Count();
        }

        public void RemoveDemoFromQueue(InQueueDemo demo)
        {
            _context.InQueueDemo.Remove(demo);
            _context.SaveChanges();
        }

        public int GetQueuePosition(InQueueDemo demo)
        {
            return _context.InQueueDemo.Count(x => x.InsertDate < demo.InsertDate);
        }

        /// <summary>
        /// Increments the number of retries for this demo and return the new number
        /// </summary>
        public int IncrementRetry(InQueueDemo demo)
        {
            var attempts = demo.RetryAttemptsOnCurrentFailure++;

            _context.SaveChanges();
            return attempts;
        }

        public InQueueDemo GetDemoById(long matchId)
        {
            return _context.InQueueDemo.Single(x => x.MatchId == matchId);
        }
    }
}