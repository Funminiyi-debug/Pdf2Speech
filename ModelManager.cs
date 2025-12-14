using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using PdfToSpeechApp.Interfaces;
using PdfToSpeechApp.Services.Infrastructure;

namespace PdfToSpeechApp;

public class ModelManager
{
    private readonly string _modelsDir;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    // Mapping of short names to base URLs (without extension)
    // We assume .onnx and .onnx.json exist at these URLs.
    private static readonly Dictionary<string, string> _knownModels = new()
    {
        { "lessac-medium", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium" },
        { "lessac-high", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/high/en_US-lessac-high" },
        { "ryan-medium", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/medium/en_US-ryan-medium" },
        { "ryan-high", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/high/en_US-ryan-high" },
        { "alan-medium", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/alan/medium/en_GB-alan-medium" },
        { "southern-low", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/southern_english_female/low/en_GB-southern_english_female-low" },
        { "aru-medium", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/aru/medium/en_GB-aru-medium"},
    };

    public ModelManager(string modelsDir, ILogger logger)
    {
        _modelsDir = modelsDir;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    // Constructor for backwards compatibility (used in DI registration)
    public ModelManager(string modelsDir) : this(modelsDir, new NullLogger())
    {
    }

    public async Task<string?> GetModelPathAsync(string modelName)
    {
        // 1. Check if it's a known model alias
        if (_knownModels.TryGetValue(modelName, out string? baseUrl))
        {
            return await EnsureModelDownloadedAsync(modelName, baseUrl);
        }

        // 2. Check if it's a direct path or filename already in models dir
        string localPath = Path.Combine(_modelsDir, modelName);
        if (File.Exists(localPath)) return localPath;

        if (File.Exists(modelName)) return modelName;

        _logger.LogError($"Unknown model '{modelName}'. Available models: {string.Join(", ", _knownModels.Keys)}");
        return null;
    }

    private async Task<string> EnsureModelDownloadedAsync(string modelName, string baseUrl)
    {
        string onnxFileName = $"{modelName}.onnx";
        string jsonFileName = $"{modelName}.onnx.json";

        string onnxPath = Path.Combine(_modelsDir, onnxFileName);
        string jsonPath = Path.Combine(_modelsDir, jsonFileName);

        if (File.Exists(onnxPath) && File.Exists(jsonPath))
        {
            return onnxPath;
        }

        _logger.Log($"Downloading model '{modelName}'...");
        Directory.CreateDirectory(_modelsDir);

        var taskNames = new[] { onnxFileName, jsonFileName };
        var downloadTasks = new (string url, string path)[]
        {
            ($"{baseUrl}.onnx", onnxPath),
            ($"{baseUrl}.onnx.json", jsonPath)
        };

        await _logger.RunDownloadProgressAsync(taskNames, async reportProgress =>
        {
            for (int i = 0; i < downloadTasks.Length; i++)
            {
                var (url, path) = downloadTasks[i];
                await DownloadFileWithProgressAsync(url, path, i, reportProgress);
            }
        });

        _logger.LogSuccess($"Model '{modelName}' downloaded successfully!");
        return onnxPath;
    }

    private async Task DownloadFileWithProgressAsync(string url, string outputPath, int taskIndex, Action<int, long, long> reportProgress)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        reportProgress(taskIndex, 0, totalBytes);

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(outputPath, FileMode.Create);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            totalRead += bytesRead;
            reportProgress(taskIndex, totalRead, totalBytes);
        }

        reportProgress(taskIndex, totalBytes, totalBytes);
    }
}
