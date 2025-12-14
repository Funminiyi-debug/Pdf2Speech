using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PdfToSpeechApp;

public class ModelManager(string modelsDir)
{
    private readonly string _modelsDir = modelsDir;
    private readonly HttpClient _httpClient = new HttpClient();

    // Mapping of short names to base URLs (without extension)
    // We assume .onnx and .onnx.json exist at these URLs.
    private static readonly Dictionary<string, string> _knownModels = new()
    {
        { "lessac-medium", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium" },
        { "lessac-high", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/high/en_US-lessac-high" },
        { "ryan-medium", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/medium/en_US-ryan-medium" },
        { "ryan-high", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/high/en_US-ryan-high" },
        { "alan-medium", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/alan/medium/en_GB-alan-medium" },
        { "southern-low", "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/southern_english_female/low/en_GB-southern_english_female-low" }
    };

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

        Console.WriteLine($"Error: Unknown model '{modelName}'. Available models: {string.Join(", ", _knownModels.Keys)}");
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

        Console.WriteLine($"Downloading model '{modelName}'...");
        Directory.CreateDirectory(_modelsDir);

        await DownloadFileAsync($"{baseUrl}.onnx", onnxPath);
        await DownloadFileAsync($"{baseUrl}.onnx.json", jsonPath);

        Console.WriteLine($"Model '{modelName}' downloaded successfully.");
        return onnxPath;
    }

    private async Task DownloadFileAsync(string url, string outputPath)
    {
        Console.WriteLine($"Downloading {url}...");
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var fs = new FileStream(outputPath, FileMode.Create);
        await response.Content.CopyToAsync(fs);
    }
}
