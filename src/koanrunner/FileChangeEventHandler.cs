using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace koanrunner {

    public class FileChangeEventHandler : BackgroundService
    {
        private ILogger<FileChangeEventHandler> _logger;
        private RunnerConfig _config;
        public static List<string> processedEvents = new List<string>();
        public BlockingCollection<FileInfo> ChangedExerciseFiles { get; }

        public FileChangeEventHandler(BlockingCollection<FileInfo> changedExerciseFiles, RunnerConfig config, ILogger<FileChangeEventHandler> logger){
            this.ChangedExerciseFiles = changedExerciseFiles;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // see https://github.com/dotnet/runtime/issues/36063 as to why the 
            // following line is used to prevent a block on the blockingcollection
            // take call. 
            await Task.Yield();

            while(!ChangedExerciseFiles.IsCompleted && !stoppingToken.IsCancellationRequested){
                FileInfo changedFileInfo = ChangedExerciseFiles.Take(stoppingToken);
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
                    new DirectoryInfo(_config.TestDirectory);
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