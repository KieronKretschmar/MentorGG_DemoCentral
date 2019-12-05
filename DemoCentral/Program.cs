using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitTransfer;

namespace DemoCentral
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            //INIT
            InitializeRabbitCommunication();

            host.Run();
        }

        private static void InitializeRabbitCommunication()
        {
            //TODO make pretty
            //TODO check with garbage collection
            RabbitInitializer.SetUpRPC();
            new RabbitCommunication.DD();
            new RabbitCommunication.SO();
            new RabbitCommunication.MatchDBI();
            new RabbitCommunication.GathererConsumer();
            new RabbitCommunication.DFW();
            new RabbitCommunication.DFWHASH();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
