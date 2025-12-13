using Microsoft.Extensions.DependencyInjection;
using PdfToSpeechApp.Interfaces;
using PdfToSpeechApp.Services.Core;

namespace PdfToSpeechApp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPdfToSpeechApp(this IServiceCollection services)
    {
        // Core infrastructure and services
        services.AddSingleton<ModelManager>(sp =>
            new ModelManager(sp.GetRequiredService<IAppConfig>().ModelsDir));

        services.AddSingleton<IPdfParser, PdfPigParser>();
        services.AddSingleton<IAudioConverter, FfmpegAudioConverter>();

        // TTS services: register concretes, then composite as ITtsService
        services.AddSingleton<PiperTtsService>(sp =>
            new PiperTtsService(
                sp.GetRequiredService<IAppConfig>().PiperPath,
                sp.GetRequiredService<ILogger>()));

        services.AddSingleton<MacSayTtsService>();

        services.AddSingleton<ITtsService>(sp =>
            new FallbackTtsService(
                sp.GetRequiredService<PiperTtsService>(),
                sp.GetRequiredService<MacSayTtsService>(),
                sp.GetRequiredService<ILogger>()));

        services.AddSingleton<IFileProcessor, PdfToSpeechProcessor>();

        services.AddSingleton<IFileMonitor>(sp =>
            new DirectoryFileMonitor(
                sp.GetRequiredService<IAppConfig>().InputDir,
                "*.pdf",
                sp.GetRequiredService<IFileProcessor>(),
                sp.GetRequiredService<ILogger>()));

        services.AddSingleton<AppEntry>();

        return services;
    }
}
