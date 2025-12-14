namespace PdfToSpeechApp.Interfaces;

public interface IAppConfig
{
    string InputDir { get; }
    string OutputDir { get; }
    string ModelsDir { get; }
    string PiperPath { get; }
    string ModelName { get; }
    string? ResolvedModelPath { get; }
    int? SpeakerId { get; }
}
