using System;
using System.Threading.Tasks;
using CliWrap;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Core;

public class FfmpegAudioConverter : IAudioConverter
{
    private readonly ILogger _logger;

    public FfmpegAudioConverter(ILogger logger)
    {
        _logger = logger;
    }

    public async Task ConvertToMp3Async(string inputWav, string outputMp3)
    {
        _logger.Log($"Converting {inputWav} to {outputMp3}...");
        try
        {
            await Cli.Wrap("ffmpeg")
                .WithArguments(args => args
                    .Add("-i")
                    .Add(inputWav)
                    .Add("-y") // Overwrite output
                    .Add(outputMp3))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error running FFmpeg: {ex.Message}", ex);
            throw;
        }
    }
}
