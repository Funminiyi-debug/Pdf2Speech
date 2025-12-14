using System;
using System.IO;
using System.Threading.Tasks;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp;

public class AppEntry(
    ILogger logger,
    IAppConfig config,
    ModelManager modelManager,
    IFileMonitor fileMonitor,
    IFileProcessor fileProcessor)
{

    public async Task RunAsync(string[] args)
    {
        logger.Log("Starting PDF to Speech App (DI Refactored)...");

        if (HasArgument(args, "--generate-sample"))
        {
            GenerateSample(config.InputDir);
            return;
        }

        string? specificFile = GetArgumentValue(args, "--process-file");

        // 2. Resolve Model (Ensure it exists)
        logger.Log($"Checking model: {config.ModelName}...");
        string? resolvedModelPath = await modelManager.GetModelPathAsync(config.ModelName);

        if (resolvedModelPath == null)
        {
            logger.LogError("Could not resolve model path. Exiting.");
            return;
        }

        // Note: PiperPath resolution and model path injection into processor is a bit tricky with DI 
        // if they are dynamic.
        // However, PdfToSpeechProcessor was injected with `resolvedModelPath` in the manual DI version.
        // In DI Container, we can't easily inject a runtime-resolved string unless we register it AFTER resolution,
        // or we inject a service that PROVIDES the path.
        // 
        // We should probably inject IAppConfig (which has PiperPath) into services.
        // And regarding ModelPath: `PdfToSpeechProcessor` needs it.
        // We can add `ResolvedModelPath` to `IAppConfig` or similar.
        // Or `PdfToSpeechProcessor` calls `ModelManager`? 
        // Let's defer that discussion to Program.cs wiring, but for now assuming dependencies are set up.
        //
        // Actually, `PdfToSpeechProcessor` depends on `resolvedModelPath` currently. 
        // We should fix `PdfToSpeechProcessor` to NOT take string in constructor, but maybe take `ModelManager`?
        // OR we set the path in Config.
        // Let's set it in Config for simplicity here if possible, or pass it to ProcessFileAsync?
        // Changing ProcessFileAsync signature requires interface change.
        // 
        // Let's update `AppConfig` to hold `ResolvedModelPath`.

        if (!string.IsNullOrEmpty(specificFile))
        {
            logger.Log($"Processing single file: {specificFile}");
            await fileProcessor.ProcessFileAsync(specificFile);
            return;
        }

        // 3. Run Monitor
        fileMonitor.StartMonitoring();
        logger.Log("Press 'q' to quit.");
        while (Console.Read() != 'q') ;
    }

    private static bool HasArgument(string[] args, string flag)
    {
        return Array.Exists(args, arg => arg == flag);
    }

    private static string? GetArgumentValue(string[] args, string flag)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == flag && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private void GenerateSample(string inputDir)
    {
        try
        {
            Directory.CreateDirectory(inputDir);
            string samplePath = Path.Combine(inputDir, "sample.pdf");
            PdfGenerator.CreateSamplePdf(samplePath);
            logger.Log($"Sample PDF generated at {samplePath}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error generating sample: {ex.Message}", ex);
        }
    }
}
