using Database.DatabaseClasses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DemoCentral
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            // Migrate the Database if it's NOT an InMemory Database.
            // (╯°□°）╯︵ ┻━┻
            using (var scope = host.Services.CreateScope())
            {
                if (scope.ServiceProvider.GetRequiredService<DemoCentralContext>().Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
                {
                    scope.ServiceProvider.GetRequiredService<DemoCentralContext>().Database.Migrate();
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
