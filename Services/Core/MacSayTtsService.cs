using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Core;

public class MacSayTtsService(
        ILogger logger
    ) : ITtsService
{
    public async Task GenerateAudioAsync(IEnumerable<string> textChunks, string outputPath, string modelPath, IProgress<int>? progress = null)
    {
        // modelPath is ignored for 'say'
        logger.Log("Using macOS 'say' command...");
        try
        {
            var pipeSource = PipeSource.Create(async (destination, cancellationToken) =>
            {
                using var writer = new StreamWriter(destination, Encoding.UTF8, leaveOpen: true);
                int count = 0;
                foreach (var chunk in textChunks)
                {
                    await writer.WriteLineAsync(chunk.AsMemory(), cancellationToken);
                    count++;
                    progress?.Report(count);
                }
            });

            await Cli.Wrap("/usr/bin/say")
                .WithArguments(args => args
                    .Add("-o")
                    .Add(outputPath)
                    .Add("--data-format=LEI16@22050")) // Reads from stdin by default if no text arg
                .WithStandardInputPipe(pipeSource)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();
        }
        catch (Exception ex)
        {
            logger.LogError($"Error running 'say': {ex.Message}", ex);
            throw;
        }
    }
}
