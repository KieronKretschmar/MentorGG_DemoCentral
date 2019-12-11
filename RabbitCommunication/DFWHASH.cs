using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DemoCentral.DatabaseClasses;
using System.Linq;
using RabbitTransfer;

namespace DemoCentral.RabbitCommunication
{
    public class DFWHASH: AbstractRPCServer
    {
        public override string QUEUE_NAME => RPCExchange.DC_DFW_HASH.QUEUE;

        public DFWHASH() : base()
        {
        }

        private bool DoDuplicateCheck(string hash)
        {
            //TODO optimize sql
            //This query is called for every demo thats downloaded, so optimizing it is going to be super worth.
            using (var context = new DemoCentralContext())
            {
                var demo = context.Demo.Where(x => x.Md5hash.Equals(hash)).SingleOrDefault();
                if (demo != null) return true;
            }
            return false;
        }

        protected override string OnMessageReceived(long matchId, string response)
        {
            string hash = response;
            bool knownHash = DoDuplicateCheck(hash);

            var res = new HashTransferModel();
            res.isDuplicate = knownHash;
            res.matchId = matchId;
            
            return res.ToJSON();
        }
    }
}
