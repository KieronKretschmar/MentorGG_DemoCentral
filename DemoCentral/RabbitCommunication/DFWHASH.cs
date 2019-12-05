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
            using (var context = new democentralContext())
            {
                var demo = context.Demo.Where(x => x.Md5hash.Equals(hash)).SingleOrDefault();
                if (demo != null) return true;
            }
            return false;
        }

        protected override byte[] OnMessageReceived(long matchId, byte[] response)
        {
            string hash = Encoding.UTF8.GetString(response);
            bool res = DoDuplicateCheck(hash);

            Console.WriteLine("Got Hash request for demo#" + matchId);

            return Encoding.UTF8.GetBytes(res.ToString());
        }
    }
}
