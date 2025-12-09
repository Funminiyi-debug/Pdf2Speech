using System;
using System.IO;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Infrastructure;

public class AppConfig : IAppConfig
{
    public string InputDir { get; }
    public string OutputDir { get; }
    public string ModelsDir { get; }
    public string PiperPath { get; private set; }
    public string ModelName { get; private set; }
    public string? ResolvedModelPath { get; private set; }

    public void SetResolvedModelPath(string path)
    {
        ResolvedModelPath = path;
    }

    public AppConfig(string[] args)
    {
        // Default paths
        InputDir = Path.GetFullPath("input");
        OutputDir = Path.GetFullPath("output");
        ModelsDir = Path.GetFullPath("models");
        PiperPath = "piper"; // Default fallback
        ModelName = "lessac-medium"; // Default

        ParseArgs(args);
    }

    public void SetPiperPath(string path)
    {
        PiperPath = path;
    }

    private void ParseArgs(string[] args)
    {
        if (args.Length > 0)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--model" && i + 1 < args.Length)
                {
                    ModelName = args[i + 1];
                    i++;
                }
                // Extend here for input/output dir args if needed
            }
        }
    }
}
