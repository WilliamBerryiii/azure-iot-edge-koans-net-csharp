using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace koanrunner
{
    class Program
    {
        static string exerciseDirectory = @"..\..\exercises";
        static string testDirectory = @"..\..\tests";
        static FileChangeEventHandler fileChangeEventHandler = null;

        static Stack<FileSystemEventArgs> eventQueue = new Stack<FileSystemEventArgs>();

        static async Task Main(string[] args)
        {
            // wire up file system watcher 

            if (!Directory.Exists(exerciseDirectory))
            {
                Console.WriteLine(Directory.GetCurrentDirectory());
                // Display the proper way to call the program.
                Console.WriteLine("Exercise Directory '{x}' was not found. Consider restoring this project with 'git checkout .'", exerciseDirectory);
                return;
            }

            // Create a new FileSystemWatcher and set its properties.
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = exerciseDirectory;

                // Watch for changes in LastWrite times
                watcher.NotifyFilter = NotifyFilters.LastWrite;

                // Watch does not yet support multiple extensions so we'll have to 
                // filter for .cs and .csproj files in the event handlers 
                // watcher.Filter = "*.cs";

                // include watching all sub-directories under the exercises folder. 
                watcher.IncludeSubdirectories = true; 

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnCreated;
                watcher.Deleted += OnDeleted;
                watcher.Renamed += OnRenamed;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                //Task.Run(() => FileChangeEventHandler.ProcessFileEvents(exerciseDirectory, testDirectory));
                fileChangeEventHandler = new FileChangeEventHandler(exerciseDirectory, testDirectory);
                await new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                services.AddHostedService<FileChangeEventHandler>(provider => fileChangeEventHandler);
                })
                .RunConsoleAsync();

            
                // Wait for the user to quit the program.
                //Console.WriteLine("Press 'q' to quit the Koan Runner.");
                //while (Console.Read() != 'q') ;
            }
        }
        
        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Handle file changed events.
            fileChangeEventHandler.AddEvent(e);
        }

        private static void OnDeleted(object source, FileSystemEventArgs e)
        {
            // what is done when a file is deleted.
            // see if the file name contains "exercise" or "quiz" to see if
            // it's one of the koans. If so, suggest the user use git
            // to restore the file. 

            Console.WriteLine($"OnDeleted File: {e.FullPath} {e.ChangeType}");
        }

        private static void OnCreated(object source, FileSystemEventArgs e)
        {
            // Alert user to the creation of a file for inclusion in a 
            // specific project. Lots of potential edge cases here.
            // what should we do??? 
            Console.WriteLine($"OnCreated File: {e.FullPath} {e.ChangeType}");
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {    // Specify what is done when a file is renamed.

            // see if the file name contains "exercise" or "quiz" to see if
            // it's one of the koans they accidentally renamed. If so, 
            // suggest the user use git to restore the file. 

            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");  
        }
    } 
}
