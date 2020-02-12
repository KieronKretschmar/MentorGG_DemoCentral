using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataBase.DatabaseClasses;
using Microsoft.EntityFrameworkCore;
using DemoCentral.RabbitCommunication;
using RabbitTransfer.Queues;
using Microsoft.Extensions.Logging;
using System;

namespace DemoCentral
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        //// This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DemoCentralContext>(options =>
                options.UseMySql(Configuration.GetValue<string>("CONNECTION_STRING")), ServiceLifetime.Singleton, ServiceLifetime.Singleton);

            services.AddControllers();

            services.AddLogging(o =>
            {
                o.AddConsole();
                o.AddDebug();
            });

            if (Configuration.GetValue<bool>("IS_MIGRATING"))
            {
                Console.WriteLine("IS_MIGRATING is true! ARE YOU STILL MIGRATING?");
                return;
            }

            services.AddSingleton<IInQueueDBInterface, InQueueDBInterface>();
            services.AddSingleton<IDemoCentralDBInterface, DemoCentralDBInterface>();


            //Read environment variables
            var AMQP_URI = Configuration.GetValue<string>("AMQP_URI");

            var AMQP_DEMODOWNLOADER = Configuration.GetValue<string>("AMQP_DEMODOWNLOADER");
            var AMQP_DEMODOWNLOADER_REPLY = Configuration.GetValue<string>("AMQP_DEMODOWNLOADER_REPLY");
            var demo_downloader_rpc_queue = new RPCQueueConnections(AMQP_URI, AMQP_DEMODOWNLOADER_REPLY, AMQP_DEMODOWNLOADER);

            var AMQP_DEMOFILEWORKER = Configuration.GetValue<string>("AMQP_DEMOFILEWORKER");
            var AMQP_DEMOFILEWORKER_REPLY = Configuration.GetValue<string>("AMQP_DEMOFILEWORKER_REPLY");
            var demo_fileworker_rpc_queue = new RPCQueueConnections(AMQP_URI, AMQP_DEMOFILEWORKER_REPLY, AMQP_DEMOFILEWORKER);

            var AMQP_GATHERER = Configuration.GetValue<string>("AMQP_GATHERER");
            var gatherer_queue = new QueueConnection(AMQP_URI, AMQP_GATHERER);

            var AMQP_SITUATIONSOPERATOR = Configuration.GetValue<string>("AMQP_SITUATIONSOPERATOR");
            var so_queue = new QueueConnection(AMQP_URI, AMQP_SITUATIONSOPERATOR);

            var AMQP_MATCHDBI = Configuration.GetValue<string>("AMQP_MATCHDBI");
            var matchDBI_queue = new QueueConnection(AMQP_URI, AMQP_MATCHDBI);

            //Add services, 
            //if 3 or more are required to initialize another one, just pass the service provider
            services.AddHostedService<MatchDBI>(services =>
            {
                return new MatchDBI(matchDBI_queue, services.GetRequiredService<IDemoCentralDBInterface>(),services.GetRequiredService<ILogger<MatchDBI>>());
            });

            services.AddHostedService<SituationsOperator>(services =>
            {
                return new SituationsOperator(so_queue, services.GetRequiredService<IInQueueDBInterface>(),services.GetRequiredService<ILogger<SituationsOperator>>());
            });

            //WORKAROUND for requesting a hostedService
            //Hosted services cant be addressed as an API, which we want to do with the PublishMessage() method
            //so we add a singleton and a hosted service, which returns the singleton instance
            // from https://github.com/aspnet/Extensions/issues/553
            services.AddSingleton<IDemoFileWorker,DemoFileWorker>(services =>
            {
                return new DemoFileWorker(demo_fileworker_rpc_queue, services);
            });
            services.AddHostedService<IDemoFileWorker>(p => p.GetRequiredService<IDemoFileWorker>());


            //WORKAROUND for requesting a hostedService
            //Hosted services cant be addressed as an API, which we want to do with the PublishMessage() method
            //so we add a singleton and a hosted service, which returns the singleton instance
            //from https://github.com/aspnet/Extensions/issues/553
            services.AddSingleton<IDemoDownloader,DemoDownloader>(services =>
            {
                return new DemoDownloader(demo_downloader_rpc_queue, services);
            });
            services.AddHostedService<IDemoDownloader>(p => p.GetRequiredService<IDemoDownloader>());

            services.AddHostedService<Gatherer>(services =>
            {
                return new Gatherer(gatherer_queue, services.GetRequiredService<IDemoCentralDBInterface>(), services.GetRequiredService<IDemoDownloader>(), services.GetRequiredService<ILogger<Gatherer>>());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
