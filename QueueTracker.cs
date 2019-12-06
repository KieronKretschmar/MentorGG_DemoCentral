using System;
using System.Linq;
using DemoCentral.Enumerals;
using DemoCentral.DatabaseClasses;
using System.Collections.Generic;


namespace DemoCentral
{
    public class QueueTracker
    {
        public QueueTracker()
        {
        }

        public static void Add(long matchId, DateTime matchDate, byte source, long uploaderID)
        {
            using (var context = new democentralContext())
            {
                context.InQueueDemo.Add(new InQueueDemo
                {
                    MatchId = matchId,
                    MatchDate = matchDate,
                    Source = source,
                    UploaderId = uploaderID,
                    InsertDate = DateTime.Now,
                    DFWQUEUE = false,
                    SOQUEUE = false
                });

                context.SaveChanges();
            }
        }

        public static void UpdateQueueStatus(long matchId, string QueueName, bool inQueue)
        {

            using (var context = new democentralContext())
            {
                var demo = context.InQueueDemo.Where(x => x.MatchId == matchId).Single();
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
                    default:
                        throw new Exception("Unknown queue");
                }


                //TODO implement better queue check, like names list etc
                List<bool> queueStates = new List<bool> { demo.DFWQUEUE, demo.SOQUEUE };
                if (!queueStates.Contains(true))
                {
                    context.InQueueDemo.Remove(demo);
                }

                context.SaveChanges();
            }
        }

        public static List<InQueueDemo> GetPlayerMatchesInQueue(long playerId)
        {
            using (var context = new democentralContext())
            {
                return context.InQueueDemo.Where(x => x.UploaderId == playerId).ToList();
            }
        }

        public static int GetTotalQueueLength()
        {
            using (var context = new democentralContext())
            {
                return context.InQueueDemo.Count();
            }
        }

        public static void RemoveDemoFromQueue(long matchId)
        {
            using (var context = new democentralContext())
            {
                var demo = context.InQueueDemo.Find(matchId);
                if (demo == null) throw new KeyNotFoundException("Demo not in Queue");
                
                context.InQueueDemo.Remove(demo);
            }
        }

        public static int GetQueuePosition(long matchId)
        {
            using (var context = new democentralContext())
            {
                var demo = context.InQueueDemo.Find(matchId);

                //Closest match in predefined exceptions
                if (demo == null) throw new KeyNotFoundException("Demo not in Queue");

                //TODO possible optimization by keeping track of row in db
                return context.InQueueDemo.Select(x => x.InsertDate).Where(x => x < demo.InsertDate).Count();
            }
        }

        public static int IncrementRetry(long matchId)
        {
            using (var context = new democentralContext())
            {
                var demo = context.InQueueDemo.Where(x => x.MatchId == matchId).Single();
                var attempts = demo.Retries++;

                context.SaveChanges();
                return attempts;
            }
        }
    }
}