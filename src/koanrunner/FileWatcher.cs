using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace koanrunner {

    public class FileWatcher : BackgroundService
    {
        private RunnerConfig _config;
        private readonly ILogger<FileWatcher> _logger;
        private FileSystemWatcher _watcher;
        private BlockingCollection<FileInfo> _changedFileEventQueue;

        public FileWatcher(BlockingCollection<FileInfo> changedFileEventQueue, RunnerConfig config, ILogger<FileWatcher> logger)
        {
            _logger = logger;
            _config = config;
            _changedFileEventQueue = changedFileEventQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service Starting");
            if (!Directory.Exists(_config.ExerciseDirectory))
            {
                _logger.LogWarning($"Please make sure the InputFolder [{_config.ExerciseDirectory}] exists, then restart the service.");
                return Task.CompletedTask;
            }

            _logger.LogInformation($"Binding Events from Input Folder: {_config.ExerciseDirectory}");
            _watcher = new FileSystemWatcher(_config.ExerciseDirectory)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                IncludeSubdirectories = true
            };


            // Add event handlers.
            _watcher.Changed += OnChanged;
            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;
            _watcher.Renamed += OnRenamed;

            // Begin watching.
            _logger.LogInformation("Starting File System Watcher for {0}", _config.ExerciseDirectory);
            _watcher.EnableRaisingEvents = true;
            _logger.LogInformation("Started File System Watcher for {0}", _config.ExerciseDirectory);

            return base.StartAsync(cancellationToken);
        }

        
        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Handle file changed events.
            _changedFileEventQueue.Add(new FileInfo(e.FullPath));
        }

        private void OnDeleted(object source, FileSystemEventArgs e)
        {
            // what is done when a file is deleted.
            // see if the file name contains "exercise" or "quiz" to see if
            // it's one of the koans. If so, suggest the user use git
            // to restore the file. 

            _logger.LogWarning($"OnDeleted File: {e.FullPath} {e.ChangeType}");
        }

        private void OnCreated(object source, FileSystemEventArgs e)
        {
            // Alert user to the creation of a file for inclusion in a 
            // specific project. Lots of potential edge cases here.
            // what should we do??? 
            _logger.LogInformation($"OnCreated File: {e.FullPath} {e.ChangeType}");
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {    // Specify what is done when a file is renamed.

            // see if the file name contains "exercise" or "quiz" to see if
            // it's one of the koans they accidentally renamed. If so, 
            // suggest the user use git to restore the file. 

            _logger.LogWarning($"File: {e.OldFullPath} renamed to {e.FullPath}");  
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Service");
            _watcher.EnableRaisingEvents = false;
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing Service");
            _watcher.Dispose();
            base.Dispose();
        }
    }
}