using System;
using System.Threading.Tasks;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Infrastructure;

/// <summary>
/// Null logger implementation for backwards compatibility when no logger is provided.
/// Writes plain text to console without formatting.
/// </summary>
public sealed class NullLogger : ILogger
{
    public void Log(string message) => Console.WriteLine(message);
    public void LogError(string message, Exception? ex = null) => Console.WriteLine($"ERROR: {message}");
    public void LogSuccess(string message) => Console.WriteLine($"SUCCESS: {message}");
    public void LogWarning(string message) => Console.WriteLine($"WARNING: {message}");
    public void LogHeader(string title) => Console.WriteLine($"=== {title} ===");
    public void LogSuccessPanel(string title, string message) => Console.WriteLine($"[{title}] {message}");
    public IProgressTracker CreateProgress(string description, int maxValue) => new NullProgressTracker();
    public IStatusContext CreateStatus(string message) => new NullStatusContext();

    public Task RunWithProgressAsync(string description, int maxValue, Func<IProgress<int>, Task> operation)
        => operation(new Progress<int>(_ => { }));

    public Task RunWithStatusAsync(string message, Func<Task> operation) => operation();

    public Task RunDownloadProgressAsync(string[] taskNames, Func<Action<int, long, long>, Task> operation)
        => operation((_, _, _) => { });

    private sealed class NullProgressTracker : IProgressTracker
    {
        public void Report(int value) { }
        public void Complete() { }
        public void Dispose() { }
    }

    private sealed class NullStatusContext : IStatusContext
    {
        public void UpdateStatus(string message) { }
        public void Dispose() { }
    }
}
