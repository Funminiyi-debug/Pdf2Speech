using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Core;

public class PiperTtsService : ITtsService
{
    private readonly string _piperPath;
    private readonly ILogger _logger;

    public PiperTtsService(string piperPath, ILogger logger)
    {
        _piperPath = piperPath;
        _logger = logger;
    }

    public async Task GenerateAudioAsync(IEnumerable<string> textChunks, string outputPath, string modelPath, IProgress<int>? progress = null)
    {
        _logger.Log($"Generating audio with Piper at {_piperPath}... Output: {outputPath}");

        try
        {
            // Create a pipe source that streams the chunks
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

            var result = await Cli.Wrap(_piperPath)
                .WithArguments(args => args
                    .Add("--model")
                    .Add(modelPath)
                    .Add("--output_file")
                    .Add(outputPath))
                .WithStandardInputPipe(pipeSource)
                .WithStandardErrorPipe(PipeTarget.ToDelegate(line => _logger.Log($"[Piper] {line}")))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (result.ExitCode != 0)
            {
                throw new Exception($"Piper exited with code {result.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error running Piper: {ex.Message}", ex);
            throw;
        }
    }
}
