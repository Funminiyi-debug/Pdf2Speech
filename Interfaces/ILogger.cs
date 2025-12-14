namespace PdfToSpeechApp.Interfaces;

/// <summary>
/// Logger abstraction that supports structured console output and progress tracking.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void Log(string message);

    /// <summary>
    /// Logs an error message with optional exception details.
    /// </summary>
    void LogError(string message, Exception? ex = null);

    /// <summary>
    /// Logs a success message (styled differently for visual emphasis).
    /// </summary>
    void LogSuccess(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    /// Displays a section header/rule.
    /// </summary>
    void LogHeader(string title);

    /// <summary>
    /// Displays a success panel with a message.
    /// </summary>
    void LogSuccessPanel(string title, string message);

    /// <summary>
    /// Creates a progress tracker for a long-running operation.
    /// </summary>
    /// <param name="description">Description of the task being tracked</param>
    /// <param name="maxValue">Maximum value for progress (e.g., total pages)</param>
    /// <returns>Progress tracker that can report progress</returns>
    IProgressTracker CreateProgress(string description, int maxValue);

    /// <summary>
    /// Creates a status context for showing a spinner with message.
    /// </summary>
    /// <param name="message">Initial status message</param>
    /// <returns>Status context that can be updated</returns>
    IStatusContext CreateStatus(string message);

    /// <summary>
    /// Runs an async operation with progress tracking.
    /// </summary>
    Task RunWithProgressAsync(string description, int maxValue, Func<IProgress<int>, Task> operation);

    /// <summary>
    /// Runs an async operation with status spinner.
    /// </summary>
    Task RunWithStatusAsync(string message, Func<Task> operation);

    /// <summary>
    /// Runs download progress tracking with multiple tasks.
    /// </summary>
    Task RunDownloadProgressAsync(string[] taskNames, Func<Action<int, long, long>, Task> operation);
}
