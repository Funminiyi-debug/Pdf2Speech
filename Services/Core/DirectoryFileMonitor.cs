using System;
using System.IO;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Core;

public class DirectoryFileMonitor(
        string path,
        string filter,
        IFileProcessor processor,
        ILogger logger
    ) : IFileMonitor
{
    private FileSystemWatcher? _watcher;

    public void StartMonitoring()
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        _watcher = new FileSystemWatcher(path, filter);
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        _watcher.Created += OnCreated;
        _watcher.EnableRaisingEvents = true;

        logger.Log($"Monitoring {path} for {filter} files...");
    }

    private async void OnCreated(object sender, FileSystemEventArgs e)
    {
        logger.Log($"New file detected: {e.Name}");
        await processor.ProcessFileAsync(e.FullPath);
    }
}
