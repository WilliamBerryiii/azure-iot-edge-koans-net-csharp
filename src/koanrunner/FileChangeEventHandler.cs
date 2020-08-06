using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace koanrunner {

    public class FileChangeEventHandler : BackgroundService
    {

        public static BlockingCollection<FileInfo> events = new BlockingCollection<FileInfo>(new ConcurrentStack<FileInfo>(), 500);
        public static List<string> processedEvents = new List<string>();

        public string ExerciseDirectory { get; }
        public string TestDirectory { get; }

        public FileChangeEventHandler(string exerciseDirectory, string testDirectory){
            this.ExerciseDirectory = exerciseDirectory;
            this.TestDirectory = testDirectory;
        }
        public void AddEvent (FileSystemEventArgs e){
            events.Add(new FileInfo(e.FullPath));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            
            while(!events.IsCompleted && !stoppingToken.IsCancellationRequested){
                FileInfo changedFileInfo = events.Take(stoppingToken);
                // exit if we've already processed this file recently
                if(processedEvents.Contains(changedFileInfo.Name)) {
                    processedEvents.Remove(changedFileInfo.Name);
                    continue;
                }
                
                // make sure the files that changed were *.cs code files
                // and if not, return early
                if (changedFileInfo.Extension != ".cs") continue; 

                var projectFiles = changedFileInfo.Directory.GetFiles("*.csproj");

                // build the projects where code files changed. 
                foreach (var codeProject in projectFiles)
                    CompilerProcRunner.CompileProject(DotNetRunOption.Build, codeProject.FullName);
                
                // test project names align with exercise csproj names, join to find 
                // test projects to run. 
                var targetTestProjectNames = projectFiles.Select(x => Path.GetFileNameWithoutExtension(x.FullName) + "Test.csproj");

                // Find the test projects for the exercise project that was updated
                var testProjects = 
                    new DirectoryInfo(this.TestDirectory);
                var foo = testProjects
                    .GetFiles("*.csproj", SearchOption.AllDirectories);

                var x = foo
                    .Where(x => targetTestProjectNames.Contains(x.Name));

                // iterate and build the test projects
                // ideally this should only happen at startup and 
                // we should check that the project is not already built
                foreach (var testProject in x)
                    CompilerProcRunner.CompileProject(DotNetRunOption.Test, testProject.FullName);

                // run the associated test projects

                // see if user deleted the "// I'm Done" line to move onto 
                // the next sample.

                // Guide users through the exercises in the order specified 
                // by the exercise order document. 

                // trigger compile for the project that changed
                // run dotnet test on the project that changed
                // return results for the project that changed
                //Console.WriteLine($"OnChanged File: {changedFileInfo.FullName}");

                processedEvents.Add(changedFileInfo.Name);

                await Task.Delay(250);
            }
        }
    }
}