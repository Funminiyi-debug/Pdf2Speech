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
        // Resolve project root regardless of current working directory (e.g., bin/Debug/net9.0)
        var projectRoot = ResolveProjectRoot(AppContext.BaseDirectory);

        // Default paths relative to project root
        InputDir = Path.Combine(projectRoot, "input");
        OutputDir = Path.Combine(projectRoot, "output");
        ModelsDir = Path.Combine(projectRoot, "models");

        // Ensure output directory exists to avoid runtime errors when writing files
        Directory.CreateDirectory(OutputDir);

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

    private static string ResolveProjectRoot(string startDir)
    {
        // Walk up the directory tree until we find an indicator of the project root.
        // Indicators: solution or csproj file, or known top-level folders (input, output, models, piper)
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            bool hasCsProj = File.Exists(Path.Combine(dir.FullName, "PdfToSpeechApp.csproj"));
            bool hasSln = File.Exists(Path.Combine(dir.FullName, "PdfToSpeechApp.sln"));
            bool hasKnownDirs = Directory.Exists(Path.Combine(dir.FullName, "input"))
                                && Directory.Exists(Path.Combine(dir.FullName, "models"));

            if (hasCsProj || hasSln || hasKnownDirs)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        // Fallback: current working directory
        return Directory.GetCurrentDirectory();
    }
}
