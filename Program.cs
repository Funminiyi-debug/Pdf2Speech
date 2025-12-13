using System;
using System.IO;
using System.Threading.Tasks;
using CliWrap;
using Microsoft.Extensions.DependencyInjection;
using PdfToSpeechApp.Interfaces;
using PdfToSpeechApp.Services.Core;
using PdfToSpeechApp.Services.Infrastructure;

namespace PdfToSpeechApp;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // 1. Bootstrap Config & Logger early
            var logger = new ConsoleLogger();
            var config = new AppConfig(args);

            // 2. Perform Pre-DI logic (Model/Piper Resolution)
            await SetupEnvironmentAsync(config, logger);

            // 3. Build DI container
            var services = new ServiceCollection();

            // register pre-created instances
            services.AddSingleton<ILogger>(logger);
            services.AddSingleton<IAppConfig>(config);

            // register application services
            services.AddPdfToSpeechApp();

            using var provider = services.BuildServiceProvider();

            // 4. Run App
            var app = provider.GetRequiredService<AppEntry>();
            await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            // Also write to stderr in case it's visible
            Console.Error.WriteLine($"FATAL CRASH: {ex}");
            Environment.Exit(1);
        }
    }

    // configure services method removed as we use CompositionRoot


    private static async Task SetupEnvironmentAsync(AppConfig config, ILogger logger)
    {
        // Resolve Piper
        await ResolvePiperPathAsync(config, logger);

        // We resolve ModelPath inside AppEntry or here?
        // AppEntry logic has code to resolve it.
        // But AppEntry depends on FileProcessor which depends on resolved path being in config.
        // So we MUST resolve it here.

        logger.Log($"Checking model: {config.ModelName}...");
        var modelManager = new ModelManager(config.ModelsDir);
        string? resolvedModelPath = await modelManager.GetModelPathAsync(config.ModelName);

        if (resolvedModelPath != null)
        {
            config.SetResolvedModelPath(resolvedModelPath);
        }
        else
        {
            // We can't exit easily here, but we set nothing. 
            // AppEntry or Processor will throw/log if null.
            logger.LogError("Warning: Could not resolve initial model path.");
        }
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

    private static void GenerateSample(string inputDir, ILogger logger)
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

    private static async Task ResolvePiperPathAsync(AppConfig config, ILogger logger)
    {
        // Check local piper
        string localPiper = Path.GetFullPath("piper/piper");
        if (File.Exists(localPiper))
        {
            if (await CheckDependencyAsync(localPiper, "--help"))
            {
                config.SetPiperPath(localPiper);
                logger.Log($"Using local piper at: {localPiper}");
                return;
            }
        }

        // Fallback to "piper" in PATH is default in AppConfig.
        // We can verify it works
        if (!await CheckDependencyAsync(config.PiperPath, "--help"))
        {
            logger.Log("Warning: 'piper' executable not found or not working. Will likely fallback to 'say'.");
        }
        else
        {
            logger.Log($"Using system piper: {config.PiperPath}");
        }
    }

    private static async Task<bool> CheckDependencyAsync(string command, string args)
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
            var result = await Cli.Wrap(command)
                .WithArguments(args)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(cts.Token);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

