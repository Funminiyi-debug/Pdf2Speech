namespace PdfToSpeechApp.Interfaces;

/// <summary>
/// Status context for displaying a temporary status with spinner.
/// </summary>
public interface IStatusContext : IDisposable
{
    /// <summary>
    /// Updates the status message.
    /// </summary>
    void UpdateStatus(string message);
}
