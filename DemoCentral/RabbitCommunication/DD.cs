using System;
using System.Collections.Generic;
using System.Linq;
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
            DemoCentralDBInterface.UpdateDownloadStatus(response.matchId, response.Success);
            if (response.Success)
            {
                DemoCentralDBInterface.AddFilePath(response.matchId, response.zippedFilePath);
            }
        }
    }
}
