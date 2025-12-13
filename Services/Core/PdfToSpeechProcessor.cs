using System;
using System.IO;
using System.Threading.Tasks;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Core;

public class PdfToSpeechProcessor : IFileProcessor
{
    private readonly IPdfParser _parser;
    private readonly ITtsService _ttsService;
    private readonly IAudioConverter _audioConverter;
    private readonly ILogger _logger;
    private readonly IAppConfig _config;
    private readonly string _resolvedModelPath;

    public PdfToSpeechProcessor(
        IPdfParser parser,
        ITtsService ttsService,
        IAudioConverter audioConverter,
        ILogger logger,
        IAppConfig config)
    {
        _parser = parser;
        _ttsService = ttsService;
        _audioConverter = audioConverter;
        _logger = logger;
        _config = config;

        if (config.ResolvedModelPath == null) throw new InvalidOperationException("Model path not resolved in config");
        _resolvedModelPath = config.ResolvedModelPath;
    }

    public async Task ProcessFileAsync(string filePath)
    {
        if (!WaitForFile(filePath))
        {
            _logger.Log($"Could not access file: {filePath}");
            return;
        }

        try
        {
            _logger.Log($"Processing {Path.GetFileName(filePath)}...");
            var textChunks = _parser.ExtractText(filePath);

            // We can't check IsNullOrWhiteSpace on IEnumerable easily without iterating.
            // But ExtractText returns an IEnumerable.
            // We just pass it to TTS.

            string baseName = Path.GetFileNameWithoutExtension(filePath);
            string wavPath = Path.Combine(_config.OutputDir, $"{baseName}.wav");
            string mp3Path = Path.Combine(_config.OutputDir, $"{baseName}.mp3");

            // Generate WAV
            await _ttsService.GenerateAudioAsync(textChunks, wavPath, _resolvedModelPath);

            if (File.Exists(wavPath))
            {
                // Convert to MP3
                await _audioConverter.ConvertToMp3Async(wavPath, mp3Path);
                _logger.Log($"Success! Audio saved to {mp3Path}");

                // Cleanup WAV
                File.Delete(wavPath);
            }
            else
            {
                _logger.Log("Failed to generate WAV file (or text was empty).");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing file: {ex.Message}", ex);
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
            catch (IOException ex)
            {
                System.Threading.Thread.Sleep(500);
            }
        }
        return false;
    }
}
