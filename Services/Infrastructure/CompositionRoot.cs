using System;
using PdfToSpeechApp.Interfaces;
using PdfToSpeechApp.Services.Core;
using PdfToSpeechApp.Services.Infrastructure;

namespace PdfToSpeechApp;

public class CompositionRoot
{
    public AppEntry CreateAppEntry(IAppConfig config, ILogger logger)
    {
        // Infrastructure
        var modelManager = new ModelManager(config.ModelsDir);

        // Core Definitions
        IPdfParser pdfParser = new PdfPigParser(logger);
        IAudioConverter audioConverter = new FfmpegAudioConverter(logger);

        // TTS Services
        var piperTts = new PiperTtsService(config.PiperPath, logger);
        var macSayTts = new MacSayTtsService(logger);

        // Composite TTS
        ITtsService ttsService = new FallbackTtsService(piperTts, macSayTts, logger);

        // Processor
        // Note: ResolvedModelPath must be set in config before calling this if it's used inside.
        // Or we pass it. The current PdfToSpeechProcessor reads from config.
        IFileProcessor fileProcessor = new PdfToSpeechProcessor(
            pdfParser,
            ttsService,
            audioConverter,
            logger,
            config
        );

        // Monitor
        IFileMonitor fileMonitor = new DirectoryFileMonitor(
            config.InputDir,
            "*.pdf",
            fileProcessor,
            logger
        );

        // App Entry
        return new AppEntry(
            logger,
            config,
            modelManager,
            fileMonitor,
            fileProcessor
        );
    }
}
