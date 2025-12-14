namespace PdfToSpeechApp.Interfaces;

/// <summary>
/// Progress tracker abstraction for long-running operations.
/// </summary>
public interface IProgressTracker : IDisposable
{
    /// <summary>
    /// Reports progress by incrementing the current value.
    /// </summary>
    void Report(int value);

    /// <summary>
    /// Completes the progress tracking.
    /// </summary>
    void Complete();
}
