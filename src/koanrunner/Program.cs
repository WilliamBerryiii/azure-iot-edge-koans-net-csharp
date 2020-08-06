using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace koanrunner
{
    class Program
    {
        static string exerciseDirectory = @"..\..\exercises";
        static string testDirectory = @"..\..\tests";
        public static BlockingCollection<FileInfo> changedFileEvents = new BlockingCollection<FileInfo>(new ConcurrentStack<FileInfo>(), 500);

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            if (!Directory.Exists(exerciseDirectory))
            {
                Log.Error("Exercise Directory '{x}' was not found. Consider restoring this project with 'git checkout .'", exerciseDirectory);
                return;
            }

            // Create a new FileSystemWatcher and set its properties.
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                Log.Information("Building File System Watcher for {0}", exerciseDirectory);

                watcher.Path = exerciseDirectory;

                // Watch for changes in LastWrite times
                watcher.NotifyFilter = NotifyFilters.LastWrite;

                // include watching all sub-directories under the exercises folder. 
                watcher.IncludeSubdirectories = true; 

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnCreated;
                watcher.Deleted += OnDeleted;
                watcher.Renamed += OnRenamed;

                // Begin watching.
                Log.Information("Starting File System Watcher for {0}", exerciseDirectory);
                watcher.EnableRaisingEvents = true;
                Log.Information("Started File System Watcher for {0}", exerciseDirectory);

                var fileChangeEventHandler = new FileChangeEventHandler(changedFileEvents, exerciseDirectory, testDirectory);
                
                // Create the Host for the application, wire up to the console lifetime
                // and start the process.
                await new HostBuilder()
                    .ConfigureServices((hostContext, services) =>
                    {
                    services.AddHostedService<FileChangeEventHandler>(provider => fileChangeEventHandler);
                    })
                    .UseSerilog()
                    .UseConsoleLifetime()
                    .RunConsoleAsync();
            }
        }
        
        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Handle file changed events.
            changedFileEvents.Add(new FileInfo(e.FullPath));
        }

        private static void OnDeleted(object source, FileSystemEventArgs e)
        {
            // what is done when a file is deleted.
            // see if the file name contains "exercise" or "quiz" to see if
            // it's one of the koans. If so, suggest the user use git
            // to restore the file. 

            Log.Warning($"OnDeleted File: {e.FullPath} {e.ChangeType}");
        }

        private static void OnCreated(object source, FileSystemEventArgs e)
        {
            // Alert user to the creation of a file for inclusion in a 
            // specific project. Lots of potential edge cases here.
            // what should we do??? 
            Log.Information($"OnCreated File: {e.FullPath} {e.ChangeType}");
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {    // Specify what is done when a file is renamed.

            // see if the file name contains "exercise" or "quiz" to see if
            // it's one of the koans they accidentally renamed. If so, 
            // suggest the user use git to restore the file. 

            Log.Warning($"File: {e.OldFullPath} renamed to {e.FullPath}");  
        }
    } 
}
