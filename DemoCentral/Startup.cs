using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataBase.DatabaseClasses;
using Microsoft.EntityFrameworkCore;
using DemoCentral.RabbitCommunication;
using RabbitCommunicationLib.Queues;
using Microsoft.Extensions.Logging;
using System;
using DemoCentral.Communication.HTTP;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace DemoCentral
{
    /// <summary>
    /// DemoCentral orchestrates the entire demo acquisition and analysis.
    /// 
    /// 
    /// Required environment variables
    /// [AMQP_URI,AMQP_DEMODOWNLOADER, AMQP_DEMODOWNLOADER_REPLY,
    ///     AMQP_DEMOFILEWORKER, AMQP_DEMOFILEWORKER_REPLY, AMQP_GATHERER,
    ///     AMQP_SITUATIONSOPERATOR, AMQP_MATCHDBI,AMQP_MANUALDEMODOWNLOAD, HTTP_USER_SUBSCRIPTION]
    /// </summary>
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
            string MYSQL_CONNECTION_STRING = GetRequiredEnvironmentVariable<string>(Configuration, "MYSQL_CONNECTION_STRING");

            services.AddDbContext<DemoCentralContext>(options =>
                options.UseMySql(MYSQL_CONNECTION_STRING), ServiceLifetime.Singleton, ServiceLifetime.Singleton);

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

            services.AddApiVersioning(o => {
                o.ReportApiVersions = true;
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
            });


            #region Swagger
            services.AddSwaggerGen(options =>
            {
                OpenApiInfo interface_info = new OpenApiInfo { Title = "[DemoCentral]", Version = "v1", };
                options.SwaggerDoc("v1", interface_info);

                // Generate documentation based on the XML Comments provided.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });
            #endregion

            if (Configuration.GetValue<bool>("DOC_GEN"))
            {
                Console.WriteLine("DOC_GEN is true! ARE YOU JUST TRYING TO BUILD THE DOCS?");
                return;
            }

            services.AddSingleton<IInQueueDBInterface, InQueueDBInterface>();
            services.AddSingleton<IDemoCentralDBInterface, DemoCentralDBInterface>();


            //Read environment variables
            var AMQP_URI = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_URI");


            var AMQP_DEMODOWNLOADER = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_DEMODOWNLOADER");
            var AMQP_DEMODOWNLOADER_REPLY = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_DEMODOWNLOADER_REPLY");
            var demoDownloaderRpcQueue = new RPCQueueConnections(AMQP_URI, AMQP_DEMODOWNLOADER_REPLY, AMQP_DEMODOWNLOADER);

            var AMQP_DEMOFILEWORKER = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_DEMOFILEWORKER");
            var AMQP_DEMOFILEWORKER_REPLY = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_DEMOFILEWORKER_REPLY");
            var demoFileworkerRpcQueue = new RPCQueueConnections(AMQP_URI, AMQP_DEMOFILEWORKER_REPLY, AMQP_DEMOFILEWORKER);

            var AMQP_GATHERER = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_GATHERER");
            var gathererQueue = new QueueConnection(AMQP_URI, AMQP_GATHERER);

            var AMQP_SITUATIONSOPERATOR = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_SITUATIONSOPERATOR");
            var soQueue = new QueueConnection(AMQP_URI, AMQP_SITUATIONSOPERATOR);

            var AMQP_MATCHDBI = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_MATCHDBI");
            var matchDBIQueue = new QueueConnection(AMQP_URI, AMQP_MATCHDBI);

            var AMQP_MANUALDEMODOWNLOAD = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_MANUALDEMODOWNLOAD");
            var manualDemoDownloadQueue = new QueueConnection(AMQP_URI, AMQP_MANUALDEMODOWNLOAD);


            var HTTP_USER_SUBSCRIPTION = GetRequiredEnvironmentVariable<string>(Configuration, "HTTP_USER_SUBSCRIPTION");

            //Add services, 
            //if 3 or more are required to initialize another one, just pass the service provider
            services.AddHostedService<MatchDBI>(services =>
            {
                return new MatchDBI(matchDBIQueue, services.GetRequiredService<IDemoCentralDBInterface>(),services.GetRequiredService<ILogger<MatchDBI>>());
            });

            services.AddHostedService<SituationsOperator>(services =>
            {
                return new SituationsOperator(soQueue, services.GetRequiredService<IInQueueDBInterface>(),services.GetRequiredService<ILogger<SituationsOperator>>());
            });

            //WORKAROUND for requesting a hostedService
            //Hosted services cant be addressed as an API, which we want to do with the PublishMessage() method
            //so we add a singleton and a hosted service, which returns the singleton instance
            // from https://github.com/aspnet/Extensions/issues/553
            services.AddSingleton<IDemoFileWorker,DemoFileWorker>(services =>
            {
                return new DemoFileWorker(demoFileworkerRpcQueue, services);
            });
            services.AddHostedService<IDemoFileWorker>(p => p.GetRequiredService<IDemoFileWorker>());


            //WORKAROUND for requesting a hostedService
            //Hosted services cant be addressed as an API, which we want to do with the PublishMessage() method
            //so we add a singleton and a hosted service, which returns the singleton instance
            //from https://github.com/aspnet/Extensions/issues/553
            services.AddSingleton<IDemoDownloader,DemoDownloader>(services =>
            {
                return new DemoDownloader(demoDownloaderRpcQueue, services);
            });

            services.AddSingleton<IUserInfoOperator, UserInfoOperator>(services =>
            {
                return new UserInfoOperator(HTTP_USER_SUBSCRIPTION, services.GetRequiredService<ILogger<UserInfoOperator>>());
            });

            services.AddHostedService<IDemoDownloader>(p => p.GetRequiredService<IDemoDownloader>());

            services.AddHostedService<Gatherer>(services =>
            {
                return new Gatherer(gathererQueue, services.GetRequiredService<IDemoCentralDBInterface>(), services.GetRequiredService<IDemoDownloader>(),services.GetRequiredService<IUserInfoOperator>(), services.GetRequiredService<ILogger<Gatherer>>(), services.GetRequiredService<IInQueueDBInterface>());
            });

            services.AddHostedService<ManualUploadReceiver>(services =>
            {
                return new ManualUploadReceiver(manualDemoDownloadQueue, services.GetRequiredService<IDemoFileWorker>(), services.GetRequiredService<IDemoCentralDBInterface>(), services.GetRequiredService<IUserInfoOperator>(), services.GetRequiredService<IInQueueDBInterface>());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider services)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            #region Swagger
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "[DemoCentral]");
            });
            #endregion

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            services.GetRequiredService<DemoCentralContext>().Database.Migrate();
        }

        /// <summary>
        /// Attempt to retrieve an Environment Variable
        /// Throws ArgumentNullException is not found.
        /// </summary>
        /// <typeparam name="T">Type to retreive</typeparam>
        private static T GetRequiredEnvironmentVariable<T>(IConfiguration config, string key)
        {
            T value = config.GetValue<T>(key);
            if (value == null)
            {
                throw new ArgumentNullException(
                    $"{key} is missing, Configure the `{key}` environment variable.");
            }
            else
            {
                return value;
            }
        }
    }
}
