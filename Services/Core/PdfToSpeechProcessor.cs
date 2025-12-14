using System;
using System.IO;
using System.Threading.Tasks;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Core;

public class PdfToSpeechProcessor(
    IPdfParser parser,
    ITtsService ttsService,
    IAudioConverter audioConverter,
    ILogger logger,
    IAppConfig config) : IFileProcessor
{
    private readonly string _resolvedModelPath = config.ResolvedModelPath
        ?? throw new InvalidOperationException("Model path not resolved in config");

    public async Task ProcessFileAsync(string filePath)
    {
        if (!WaitForFile(filePath))
        {
            logger.Log($"Could not access file: {filePath}");
            return;
        }

        try
        {
            var fileName = Path.GetFileName(filePath);

            // Display header for the file being processed
            logger.LogHeader($"Processing: {fileName}");

            var result = parser.ExtractText(filePath);
            var totalPages = result.TotalPages;
            var textChunks = result.Pages;

            string baseName = Path.GetFileNameWithoutExtension(filePath);
            string wavPath = Path.Combine(config.OutputDir, $"{baseName}.wav");
            string mp3Path = Path.Combine(config.OutputDir, $"{baseName}.mp3");

            logger.Log($"Detected {totalPages} pages. Starting conversion...");

            // Run TTS with progress tracking
            await logger.RunWithProgressAsync(
                "Converting pages to speech",
                totalPages,
                async progress => await ttsService.GenerateAudioAsync(textChunks, wavPath, _resolvedModelPath, progress));

            if (File.Exists(wavPath))
            {
                // Convert to MP3 with status spinner
                await logger.RunWithStatusAsync("Converting to MP3...", async () =>
                {
                    await audioConverter.ConvertToMp3Async(wavPath, mp3Path);
                });

                // Show success panel
                logger.LogSuccessPanel("Success", $"Audio saved to: {mp3Path}");

                // Cleanup WAV
                File.Delete(wavPath);
            }
            else
            {
                logger.LogWarning("Failed to generate WAV file (or text was empty).");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error processing file: {ex.Message}", ex);
        }
    }

    private bool WaitForFile(string fullPath)
    {
        for (int i = 0; i < 10; i++)
        {
            try
            {
                using var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                System.Threading.Thread.Sleep(500);
            }
        }
        return false;
    }
}