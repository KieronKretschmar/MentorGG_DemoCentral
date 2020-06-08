using System;
using System.Linq;
using RabbitCommunicationLib.Enums;
using Database.DatabaseClasses;
using System.Collections.Generic;


namespace DemoCentral
{
    /// <summary>
    /// Interface for the InQueueDemo table of the database
    /// </summary>
    public interface IInQueueTableInterface
    {
        /// <summary>
        /// Add a new demo to a queue.
        /// </summary>
        InQueueDemo Add(long matchId, Queue currentQueue);
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
        /// Update the current queue.
        void UpdateCurrentQueue(InQueueDemo demo, Queue queue);

        /// <summary>
        /// Remove the item from the InQueueDemo table.
        /// </summary>
        void Remove(InQueueDemo demo);

        /// <summary>
        /// Tries to remove the demo from the InQueueDemo table.
        /// </summary>
        /// <param name="matchId"></param>
        /// <returns></returns>
        void TryRemove(long matchId);
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

        public InQueueDemo Add(long matchId, Queue currentQueue)
        {
            var newDemo = new InQueueDemo
            {
                MatchId = matchId,
                CurrentQueue = currentQueue,
            };

            _context.InQueueDemo.Add(newDemo);

            _context.SaveChanges();

            return newDemo;
        }

        public void UpdateCurrentQueue(InQueueDemo demo, Queue queue)
        {
            demo.CurrentQueue = queue;
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
            return _context.InQueueDemo.Count(x => x.Demo.UploadDate < demo.Demo.UploadDate);
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

        public void Remove(InQueueDemo demo)
        {
            _context.InQueueDemo.Remove(demo);
            _context.SaveChanges();
        }

        public void TryRemove(long matchId)
        {
            var entry = _context.InQueueDemo.SingleOrDefault(x => x.MatchId == matchId);
            if (entry != null)
            {
                Remove(entry);
            }
        }
    }
}