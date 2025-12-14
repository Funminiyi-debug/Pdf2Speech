using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Core;

public class PiperTtsService(string piperPath, ILogger logger, int? speakerId = null) : ITtsService
{
    private const string PiperStdOutPrefix = "[Piper:stdout] ";
    private const string PiperStdErrPrefix = "[Piper:stderr] ";
    private const string PiperLogPrefix = "[Piper] ";
    private const string FfmpegStdOutPrefix = "[FFmpeg:stdout] ";
    private const string FfmpegStdErrPrefix = "[FFmpeg:stderr] ";

    private readonly string _piperPath = string.IsNullOrWhiteSpace(piperPath)
        ? throw new ArgumentException("Piper path must be provided", nameof(piperPath))
        : piperPath;

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly int? _speakerId = speakerId;

    private sealed record PartsContext(string PartsDir, List<string> PartFiles);

    public async Task GenerateAudioAsync(IEnumerable<string> textChunks, string outputPath, string modelPath, IProgress<int>? progress = null)
    {
        ValidateInputs(textChunks, outputPath, modelPath);
        _logger.Log($"{PiperLogPrefix}Generating audio per-page... Piper: {_piperPath} Output: {outputPath}");

        try
        {
            var parts = PreparePartsContext(outputPath);

            int pageIndex = 0;
            int completed = 0;

            foreach (var chunk in textChunks)
            {
                pageIndex++;
                if (ShouldSkipChunk(chunk))
                {
                    _logger.Log($"{PiperLogPrefix}Skipping empty page {pageIndex}");
                    progress?.Report(++completed);
                    continue;
                }

                var partPath = BuildPartPath(parts.PartsDir, pageIndex);
                await SynthesizePageAsync(chunk!, partPath, modelPath, pageIndex);

                EnsureFileExists(partPath, $"Piper failed to produce output for page {pageIndex}");
                parts.PartFiles.Add(partPath);

                progress?.Report(++completed);
            }

            if (parts.PartFiles.Count == 0)
            {
                _logger.Log($"{PiperLogPrefix}No audio parts produced (all pages empty?). Skipping concat.");
                return;
            }

            await ConcatenatePartsAsync(parts.PartFiles, outputPath, parts.PartsDir);
            CleanupPartsSafe(parts.PartFiles, parts.PartsDir);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error running Piper: {ex.Message}", ex);
            throw;
        }
    }

    private void ValidateInputs(IEnumerable<string> textChunks, string outputPath, string modelPath)
    {
        ArgumentNullException.ThrowIfNull(textChunks);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);
    }

    private static PartsContext PreparePartsContext(string outputPath)
    {
        var outDir = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
        var baseName = Path.GetFileNameWithoutExtension(outputPath);
        var partsDir = Path.Combine(outDir, $"{baseName}_parts");
        Directory.CreateDirectory(partsDir);
        return new PartsContext(partsDir, new List<string>());
    }

    private static bool ShouldSkipChunk(string? chunk) => string.IsNullOrWhiteSpace(chunk?.Trim());

    private static string BuildPartPath(string partsDir, int pageIndex) => Path.Combine(partsDir, $"part_{pageIndex:D4}.wav");

    private async Task SynthesizePageAsync(string chunk, string partPath, string modelPath, int pageIndex)
    {
        var trimmed = chunk.Trim();
        _logger.Log($"{PiperLogPrefix}Synthesizing page {pageIndex} -> {partPath}");

        var speakerArg = _speakerId.HasValue ? $" --speaker {_speakerId.Value}" : "";
        var argsPreview = $"--model \"{modelPath}\"{speakerArg} --output_file \"{partPath}\"";
        _logger.Log($"{PiperLogPrefix}Command: {_piperPath} {argsPreview}");

        var result = await Cli.Wrap(_piperPath)
            .WithArguments(args =>
            {
                args.Add("--model").Add(modelPath);
                if (_speakerId.HasValue)
                {
                    args.Add("--speaker").Add(_speakerId.Value.ToString());
                }
                args.Add("--output_file").Add(partPath);
            })
            .WithStandardInputPipe(PipeSource.FromString(trimmed + "\n"))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => _logger.Log(Concat(PiperStdOutPrefix, line))))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line => _logger.Log(Concat(PiperStdErrPrefix, line))))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        _logger.Log($"{PiperLogPrefix}Page {pageIndex} exited with code {result.ExitCode}");
        if (result.ExitCode != 0)
        {
            throw new Exception($"Piper failed on page {pageIndex} with exit code {result.ExitCode}");
        }
    }

    private async Task ConcatenatePartsAsync(List<string> partFiles, string outputPath, string partsDir)
    {
        var listFile = Path.Combine(partsDir, "list.txt");
        await WriteConcatListAsync(partFiles, listFile);

        _logger.Log($"[FFmpeg] Concatenating {partFiles.Count} parts into {outputPath}");
        var ffArgsPreview = $"-f concat -safe 0 -i \"{listFile}\" -y -c copy \"{outputPath}\"";
        _logger.Log($"[FFmpeg] Command: ffmpeg {ffArgsPreview}");

        var concatResult = await Cli.Wrap("ffmpeg")
            .WithArguments(args => args
                .Add("-f").Add("concat")
                .Add("-safe").Add("0")
                .Add("-i").Add(listFile)
                .Add("-y")
                .Add("-c").Add("copy")
                .Add(outputPath))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => _logger.Log(Concat(FfmpegStdOutPrefix, line))))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line => _logger.Log(Concat(FfmpegStdErrPrefix, line))))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        _logger.Log($"[FFmpeg] Concat exited with code {concatResult.ExitCode}");
        if (concatResult.ExitCode != 0 || !File.Exists(outputPath))
        {
            throw new Exception($"FFmpeg concat failed with exit code {concatResult.ExitCode}");
        }

        // Remove list file after successful concat
        TryDeleteFile(listFile);
    }

    private static async Task WriteConcatListAsync(IEnumerable<string> files, string listFile)
    {
        await using var writer = new StreamWriter(listFile, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        foreach (var file in files)
        {
            var quoted = file.Replace("'", "'\\''");
            await writer.WriteLineAsync($"file '{quoted}'");
        }
    }

    private void CleanupPartsSafe(IEnumerable<string> partFiles, string partsDir)
    {
        try
        {
            foreach (var f in partFiles)
            {
                TryDeleteFile(f);
            }
            TryDeleteDirectory(partsDir);
        }
        catch (Exception cleanupEx)
        {
            _logger.Log($"Cleanup warning: {cleanupEx.Message}");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { /* ignore */ }
    }

    private static void EnsureFileExists(string filePath, string message)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException(message, filePath);
    }

    private static string Concat(string prefix, string? line) => prefix + (line ?? string.Empty);
}
