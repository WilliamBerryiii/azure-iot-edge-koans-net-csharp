using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace koanrunner
{
    class Program
    {
        static string exerciseDirectory = @"..\..\exercises";
        static string testDirectory = @"..\..\tests";
        private static BlockingCollection<FileInfo> _changedFileEvents = new BlockingCollection<FileInfo>(new ConcurrentStack<FileInfo>(), 500);

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            // Create the Host for the application, wire up to the console lifetime
            // and start the process.
            await new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    
                    services.AddLogging();
                    services.AddScoped<RunnerConfig>(provider => new RunnerConfig(exerciseDirectory, testDirectory));
                    services.AddScoped<BlockingCollection<FileInfo>>(provider => _changedFileEvents);
                    services.AddHostedService<FileWatcher>();
                    services.AddHostedService<FileChangeEventHandler>();
                    
                })
                .UseSerilog()
                .UseConsoleLifetime()
                .RunConsoleAsync();
        }
    } 
}
