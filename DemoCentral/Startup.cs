using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataBase.DatabaseClasses;
using MySql.Data.MySqlClient;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DemoCentral.RabbitCommunication;


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
                options.UseMySql(Configuration.GetConnectionString("DemoCentralDB")));
            services.AddControllers();

            services.AddScoped<IInQueueDBInterface, InQueueDBInterface>();
            services.AddScoped<IDemoCentralDBInterface, DemoCentralDBInterface>();

            //    var AMQP_URI = Configuration.GetValue<string>("AMQP_URI");

            //    var AMQP_DEMODOWNLOADER = Configuration.GetValue<string>("AMQP_DEMODOWNLOADER");
            //    var AMQP_DEMODOWNLOADER_REPLY = Configuration.GetValue<string>("AMQP_DEMODOWNLOADER_REPLY");
            //    var demo_downloader_rpc_queue = new RPCQueueConnections(AMQP_URI, AMQP_DEMODOWNLOADER_REPLY, AMQP_DEMODOWNLOADER);

            //    var AMQP_DEMOFILEWORKER = Configuration.GetValue<string>("AMQP_DEMOFILEWORKER");
            //    var AMQP_DEMOFILEWORKER_REPLY = Configuration.GetValue<string>("AMQP_DEMOFILEWORKER_REPLY");
            //    var demo_fileworker_rpc_queue = new RPCQueueConnections(AMQP_URI, AMQP_DEMOFILEWORKER_REPLY, AMQP_DEMOFILEWORKER);

            //    var AMQP_GATHERER = Configuration.GetValue<string>("AMQP_GATHERER");
            //    var gatherer_queue = new QueueConnection(AMQP_URI, AMQP_GATHERER);

            //    var AMQP_SITUATIONSOPERATOR = Configuration.GetValue<string>("AMQP_SITUATIONSOPERATOR");
            //    var so_queue = new QueueConnection(AMQP_URI, AMQP_SITUATIONSOPERATOR);

            //    var AMQP_MATCHDBI = Configuration.GetValue<string>("AMQP_MATCHDBI");
            //    var matchDBI_queue = new QueueConnection(AMQP_URI, AMQP_MATCHDBI);


            //    //TODO GetConnection and Initalize properly
            //    services.AddHostedService<MatchDBI>(services =>
            //    {
            //        return new MatchDBI(matchDBI_queue, services.GetRequiredService<IDemoCentralDBInterface>());
            //    });

            //    services.AddHostedService<DemoFileWorker>(services =>
            //    {
            //        return new DemoFileWorker(demo_fileworker_rpc_queue, services);
            //    });
            //    services.AddHostedService<Gatherer>(services =>
            //    {
            //        return new Gatherer(gatherer_queue, services.GetRequiredService<IDemoCentralDBInterface>(), services.GetRequiredService<DemoDownloader>());
            //    });
            //    services.AddHostedService<SituationsOperator>(services =>
            //    {
            //        return new SituationsOperator(so_queue, services.GetRequiredService<IInQueueDBInterface>());
            //    });
            //    services.AddHostedService<DemoDownloader>(services =>
            //    {
            //        return new DemoDownloader(demo_downloader_rpc_queue, services);
            //    });
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
