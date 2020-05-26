using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Database.DatabaseClasses;
using Microsoft.EntityFrameworkCore;
using DemoCentral.Communication;
using RabbitCommunicationLib.Queues;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using RabbitCommunicationLib.TransferModels;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.Producer;
using RabbitCommunicationLib.Extensions;
using DemoCentral.Helpers;
using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
using System.Net.Http;
using DemoCentral.Communication.MessageProcessors;
using DemoCentral.Communication.RabbitConsumers;

namespace DemoCentral
{
    /// <summary>
    /// DemoCentral orchestrates the entire demo acquisition and analysis.
    /// </summary>
    public class Startup
    {
        private bool IsDevelopment => Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == Environments.Development;

        /// <summary>
        /// Amount of times to attempt a successful MySQL connection on startup.
        /// </summary>
        const int MYSQL_RETRY_LIMIT = 3;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        //// This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region Controllers
            services.AddControllers()
                .AddNewtonsoftJson(x =>
                {
                    // Serialize JSON using the Member CASE!
                    x.UseMemberCasing();
                    // Serialize longs (steamIds) as strings
                    x.SerializerSettings.Converters.Add(new LongToStringConverter());
                });
            #endregion

            #region Logging
            services.AddLogging(o =>
            {
                o.AddConsole(o => o.TimestampFormat = "[yyyy-MM-dd HH:mm:ss zzz] ");

                //Filter out ASP.Net and EFCore logs of LogLevel lower than LogLevel.Warning
                o.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                o.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
                o.AddFilter("Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker", LogLevel.Warning);
                o.AddFilter("Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor", LogLevel.Warning);
                o.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
                o.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);
                o.AddFilter("Microsoft.AspNetCore.Mvc.StatusCodeResult", LogLevel.Warning);
            });
            #endregion

            #region Mysql Database
            string MYSQL_CONNECTION_STRING = GetOptionalEnvironmentVariable<string>(Configuration, "MYSQL_CONNECTION_STRING", null);
            if (MYSQL_CONNECTION_STRING != null)
            {
                services.AddDbContext<DemoCentralContext>(o =>
                {
                    o.UseMySql(MYSQL_CONNECTION_STRING, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(MYSQL_RETRY_LIMIT);
                    });

                }, ServiceLifetime.Scoped);
            }
            // Use InMemoryDatabase if the connectionstring is not set in a DEV enviroment
            else if (IsDevelopment)
            {
                services.AddDbContext<DemoCentralContext>(o =>
                {
                    o.UseInMemoryDatabase("MyTemporaryDatabase");

                }, ServiceLifetime.Scoped);

                Console.WriteLine("WARNING: Using InMemoryDatabase!");
            }
            else
            {
                throw new ArgumentException(
                    "MySqlConnectionString is missing, configure the `MYSQL_CONNECTION_STRING` enviroment variable.");
            }

            if (GetOptionalEnvironmentVariable<bool>(Configuration, "IS_MIGRATING", false))
            {
                Console.WriteLine("IS_MIGRATING is true! ARE YOU STILL MIGRATING?");
                return;
            }
            #endregion

            #region API Versioning
            services.AddApiVersioning(o =>
            {
                o.ReportApiVersions = true;
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
            });
            #endregion

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

            if (GetOptionalEnvironmentVariable<bool>(Configuration, "DOC_GEN", false))
            {
                Console.WriteLine("WARNING: DOC_GEN is true. Only building the swagger docs. This build is non-functional.");
                return;
            }
            #endregion

            #region Database Interfaces
            services.AddTransient<IInQueueTableInterface, InQueueTableInterface>();
            services.AddTransient<IDemoTableInterface, DemoTableInterface>();
            #endregion

            #region Blob Storage
            var BLOBSTORAGE_CONNECTION_STRING = GetRequiredEnvironmentVariable<string>(Configuration, "BLOBSTORAGE_CONNECTION_STRING");
            services.AddTransient<IBlobStorage>(services =>
            {
                return new BlobStorage(BLOBSTORAGE_CONNECTION_STRING, services.GetRequiredService<ILogger<BlobStorage>>());
            });
            #endregion

            #region Http related services
            var MENTORINTERFACE_BASE_ADDRESS = GetRequiredEnvironmentVariable<string>(Configuration, "MENTORINTERFACE_BASE_ADDRESS");
            services.AddHttpClient("mentor-interface", c =>
            {
                c.BaseAddress = new Uri(MENTORINTERFACE_BASE_ADDRESS);
            });

            services.AddTransient<IUserIdentityRetriever>(services =>
            {
                if (MENTORINTERFACE_BASE_ADDRESS == "mock")
                    return new MockUserInfoGetter(services.GetRequiredService<ILogger<MockUserInfoGetter>>());

                return new UserIdentityRetriever(services.GetRequiredService<IHttpClientFactory>(), services.GetRequiredService<ILogger<UserIdentityRetriever>>());
            });
            #endregion

            #region Rabbit - General
            var AMQP_URI = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_URI");
            #endregion

            #region Rabbit - Consumers
            // New demos from Gatherers
            var AMQP_GATHERER = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_GATHERER");
            var gathererQueue = new QueueConnection(AMQP_URI, AMQP_GATHERER);
            services.AddHostedService<DemoInsertInstructionConsumer>(services =>
            {
                return new DemoInsertInstructionConsumer(
                    services,
                    services.GetRequiredService<ILogger<DemoInsertInstructionConsumer>>(),
                    gathererQueue);
            });

            // New downloads from ManualDemoDownloader
            var AMQP_MANUALDEMODOWNLOAD = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_MANUALDEMODOWNLOAD");
            var manualDemoDownloadQueue = new QueueConnection(AMQP_URI, AMQP_MANUALDEMODOWNLOAD);
            services.AddHostedService<ManualDownloadInsertInstructionConsumer>(services =>
            {
                return new ManualDownloadInsertInstructionConsumer(
                    services,
                    services.GetRequiredService<ILogger<ManualDownloadInsertInstructionConsumer>>(),
                    manualDemoDownloadQueue
                );
            });

            // Download Reports from DemoDownloader
            var AMQP_DEMODOWNLOADER_REPLY = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_DEMODOWNLOADER_REPLY");
            var demoDownloaderReportQueue = new QueueConnection(AMQP_URI, AMQP_DEMODOWNLOADER_REPLY);
            services.AddHostedService<DemoDownloadReportConsumer>(services =>
            {
                return new DemoDownloadReportConsumer(services, services.GetRequiredService<ILogger<DemoDownloadReportConsumer>>(), demoDownloaderReportQueue);
            });

            // Analyze Reports from DemoFileWorker
            var AMQP_DEMOFILEWORKER_REPLY = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_DEMOFILEWORKER_REPLY");
            var demoFileWorkerReportQueue = new QueueConnection(AMQP_URI, AMQP_DEMOFILEWORKER_REPLY);
            services.AddHostedService<DemoAnalyzeReportReportConsumer>(services =>
            {
                return new DemoAnalyzeReportReportConsumer(services, services.GetRequiredService<ILogger<DemoAnalyzeReportReportConsumer>>(), demoFileWorkerReportQueue);
            });

            // Upload Reports from MatchWriter
            var AMQP_MATCHWRITER_UPLOAD_REPORT = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_MATCHWRITER_UPLOAD_REPORT");
            var matchwriterUploadReportQueue = new QueueConnection(AMQP_URI, AMQP_MATCHWRITER_UPLOAD_REPORT);
            services.AddHostedService<MatchDatabaseInsertionReportConsumer>(services =>
            {
                return new MatchDatabaseInsertionReportConsumer(services, services.GetRequiredService<ILogger<MatchDatabaseInsertionReportConsumer>>(), matchwriterUploadReportQueue);
            });

            // Removal Reports from MatchWriter
            var AMQP_MATCHWRITER_DEMO_REMOVAL_REPLY = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_MATCHWRITER_DEMO_REMOVAL_REPLY");
            var matchwriterRemovalReportQueue = new QueueConnection(AMQP_URI, AMQP_MATCHWRITER_DEMO_REMOVAL_REPLY);
            services.AddHostedService<MatchWriterRemovalReportConsumer>(services =>
            {
                return new MatchWriterRemovalReportConsumer(services, services.GetRequiredService<ILogger<MatchWriterRemovalReportConsumer>>(), matchwriterRemovalReportQueue);
            });
            #endregion

            #region Rabbit - Producers
            // To DemoCentral (for matches uploaded via Browser-Extension)
            services.AddProducer<DemoInsertInstruction>(AMQP_URI, AMQP_GATHERER);

            // To DemoDownloader
            var AMQP_DEMODOWNLOADER = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_DEMODOWNLOADER");
            services.AddProducer<DemoDownloadInstruction>(AMQP_URI, AMQP_DEMODOWNLOADER);

            // To DemoFileWorker
            var AMQP_DEMOFILEWORKER = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_DEMOFILEWORKER");
            services.AddProducer<DemoAnalyzeInstruction>(AMQP_URI, AMQP_DEMOFILEWORKER);

            // To MatchData-Exchange
            var AMQP_FANOUT_EXCHANGE_NAME = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_FANOUT_EXCHANGE_NAME");
            services.AddFanoutProducer<MatchDatabaseInsertionInstruction>(AMQP_URI, AMQP_FANOUT_EXCHANGE_NAME);

            // To SituationOperator
            var AMQP_SITUATION_OPERATOR = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_SITUATION_OPERATOR");
            services.AddProducer<SituationExtractionReport>(AMQP_URI, AMQP_SITUATION_OPERATOR);

            // Removal-Instructions to MatchWriter
            var AMQP_MATCHWRITER_DEMO_REMOVAL = GetRequiredEnvironmentVariable<string>(Configuration, "AMQP_MATCHWRITER_DEMO_REMOVAL");
            services.AddProducer<DemoRemovalInstruction>(AMQP_URI, AMQP_MATCHWRITER_DEMO_REMOVAL);
            #endregion

            #region Rabbit - MessageProcessors
            services.AddTransient<DemoInsertInstructionProcessor>();
            services.AddTransient<DemoDownloadReportProcessor>();
            services.AddTransient<DemoAnalyzeReportProcessor>();
            services.AddTransient<ManualDownloadInsertInstructionProcessor>();
            services.AddTransient<MatchDatabaseInsertionReportProcessor>();
            services.AddTransient<SituationExtractionReportProcessor>();
            #endregion
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

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            #region Run Migrations
            // migrate if this is not an inmemory database
            if (services.GetRequiredService<DemoCentralContext>().Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                services.GetRequiredService<DemoCentralContext>().Database.Migrate();
            }
            #endregion
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

        /// <summary>
        /// Attempt to retrieve an Environment Variable
        /// Returns default value if not found.
        /// </summary>
        /// <typeparam name="T">Type to retreive</typeparam>
        private static T GetOptionalEnvironmentVariable<T>(IConfiguration config, string key, T defaultValue)
        {
            var stringValue = config.GetSection(key).Value;
            try
            {
                T value = (T)Convert.ChangeType(stringValue, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
                return value;
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine($"Env var [ {key} ] not specified. Defaulting to [ {defaultValue} ]");
                return defaultValue;
            }
        }
    }
}
