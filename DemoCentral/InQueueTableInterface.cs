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
        void ResetRetry(InQueueDemo demo);

        /// <summary>
        /// Update the current queue, in Queue is set to Queue.UnQueued
        /// The Demo is removed from the InQueue table
        /// </summary>
        /// <param name="demo"></param>
        /// <param name="queue"></param>
        void UpdateCurrentQueue(InQueueDemo demo, Queue queue);
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

        public void UpdateCurrentQueue(InQueueDemo demo, Queue queue)
        {
            if (demo.CurrentQueue == Queue.UnQueued)
            {
                _context.InQueueDemo.Remove(demo);
            }
            else
            {
                demo.CurrentQueue = queue;
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

        public void ResetRetry(InQueueDemo demo)
        {
            demo.RetryAttemptsOnCurrentFailure = 0;
            _context.SaveChanges();
        }

        public InQueueDemo GetDemoById(long matchId)
        {
            return _context.InQueueDemo.Single(x => x.MatchId == matchId);
        }
    }
}