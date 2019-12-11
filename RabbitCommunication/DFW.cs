﻿using DemoCentral.Enumerals;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DemoCentral.DatabaseClasses;
using System;
using System.IO;
using RabbitTransfer;

namespace DemoCentral.RabbitCommunication
{
    public class DFW : AbstractRPCClient<DFW2DCModel>
    {
        public override string QUEUE_NAME => RPCExchange.DC_DFW.QUEUE;

        public override string REPLY_QUEUE => RPCExchange.DC_DFW.REPLY_QUEUE;

        public DFW() : base()
        {
        }

        public override void HandleReplyQueue(long matchId, DFW2DCModel response)
        {
            updateDBEntryFromFileWorkerResponse(response);
        }

        public new Task<DFW2DCModel> SendNewDemo(long matchId, string demo, CancellationToken token = default(CancellationToken))
        {
            InQueueDBInterface.UpdateQueueStatus(matchId, "DFW", true);
            return base.SendNewDemo(matchId, demo, token);
        }

        
        private void updateDBEntryFromFileWorkerResponse(DFW2DCModel response)
        {
            using (var context = new DemoCentralContext())
            {
                Demo demo = context.Demo.Where(x => x.MatchId == response.matchId).Single();
                if (!response.Unzipped)
                {
                    demo.FileStatus = (byte)FileStatus.UNZIPFAILED;
                }
                else if (response.DuplicateChecked && response.IsDuplicate)
                {
                    //TODO Put in extra table if same match uploaded by different persons
                    context.Demo.Remove(demo);
                }
                else if (response.UploadedToDb)
                {
                    demo.DemoAnalyzerStatus = response.Success ? (byte)DemoAnalyzerStatus.Finished : (byte)DemoAnalyzerStatus.Failed;
                    demo.DemoAnalyzerVersion = response.AnalyzerVersion;
                    demo.Md5hash = response.Hash;
                    demo.FileStatus = response.Success ? (byte)FileStatus.ANALYZED : (byte)FileStatus.ANALYZERFAILED;
                    demo.FilePath = response.zippedFilePath;
                    demo.FileName = Path.GetFileName(response.zippedFilePath);

                    InQueueDBInterface.UpdateQueueStatus(response.matchId, "DFW", false);
                }

                Console.WriteLine("DemoEntry updated");
                context.SaveChanges();
            }
        }
    }
}