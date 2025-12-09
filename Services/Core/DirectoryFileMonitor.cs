using System;
using System.IO;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Core;

public class DirectoryFileMonitor : IFileMonitor
{
    private readonly string _path;
    private readonly string _filter;
    private readonly IFileProcessor _processor;
    private readonly ILogger _logger;
    private FileSystemWatcher? _watcher;

    public DirectoryFileMonitor(string path, string filter, IFileProcessor processor, ILogger logger)
    {
        _path = path;
        _filter = filter;
        _processor = processor;
        _logger = logger;
    }

    public void StartMonitoring()
    {
        if (!Directory.Exists(_path))
        {
            Directory.CreateDirectory(_path);
        }

        _watcher = new FileSystemWatcher(_path, _filter);
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        _watcher.Created += OnCreated;
        _watcher.EnableRaisingEvents = true;

        _logger.Log($"Monitoring {_path} for {_filter} files...");
    }

    private async void OnCreated(object sender, FileSystemEventArgs e)
    {
        _logger.Log($"New file detected: {e.Name}");
        await _processor.ProcessFileAsync(e.FullPath);
    }
}
