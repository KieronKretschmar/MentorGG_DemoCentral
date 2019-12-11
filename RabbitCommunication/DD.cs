using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitTransfer;


namespace DemoCentral.RabbitCommunication
{
    public class DD : AbstractRPCClient<DD_DC_Model>
    {
        public override string QUEUE_NAME => RPCExchange.DC_DD.QUEUE;

        public override string REPLY_QUEUE => RPCExchange.DC_DD.REPLY_QUEUE;

        public override void HandleReplyQueue(long matchId, DD_DC_Model response)
        {
            DemoCentralDBInterface.UpdateDownloadStatus(matchId, response.Success);
            if (response.Success)
            {
                DemoCentralDBInterface.AddFilePath(matchId, response.zippedFilePath);

                InQueueDBInterface.UpdateQueueStatus(matchId, "DD", false);

                //TODO send to DFW
            }
            else
            {
                var downloadUrl = DemoCentralDBInterface.SetDownloadRetryingAndGetDownloadPath(matchId);
                int attempts = InQueueDBInterface.IncrementRetry(matchId);

                if (attempts >= 3)
                {
                    DemoCentralDBInterface.RemoveDemo(matchId);
                    InQueueDBInterface.RemoveDemoFromQueue(matchId);
                }
                else
                {
                    var resendModel = new DC_DD_Model
                    {
                        matchId = matchId,
                        DownloadPath = downloadUrl,
                    };

                    this.SendNewDemo(resendModel.matchId, resendModel.ToJSON());
                }
            }
        }
    }
}

