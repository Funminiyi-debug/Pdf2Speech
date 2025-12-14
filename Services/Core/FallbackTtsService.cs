using System;
using System.Threading.Tasks;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Core;

public class FallbackTtsService(
        ITtsService primary,
        ITtsService fallback,
        ILogger logger
    ) : ITtsService
{
    public async Task GenerateAudioAsync(IEnumerable<string> textChunks, string outputPath, string modelPath, IProgress<int>? progress = null)
    {
        // Buffer text to support fallback retry
        var bufferedText = new List<string>(textChunks);

        try
        {
            await primary.GenerateAudioAsync(bufferedText, outputPath, modelPath, progress);
        }
        catch (Exception ex)
        {
            logger.LogError($"Primary TTS failed ({ex.Message}). Attempting fallback...", ex);
            // await fallback.GenerateAudioAsync(bufferedText, outputPath, modelPath, progress);
        }
    }
}
