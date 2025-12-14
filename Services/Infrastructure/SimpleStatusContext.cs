using System;
using PdfToSpeechApp.Interfaces;
using Spectre.Console;

namespace PdfToSpeechApp.Services.Infrastructure;

/// <summary>
/// Simple status context implementation for displaying status messages.
/// </summary>
internal sealed class SimpleStatusContext : IStatusContext
{
    private readonly bool _isTestMode;

    public SimpleStatusContext(string message, bool isTestMode)
    {
        _isTestMode = isTestMode;
        if (isTestMode)
        {
            Console.WriteLine($"[STATUS] {message}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");
        }
    }

    public void UpdateStatus(string message)
    {
        if (_isTestMode)
        {
            Console.WriteLine($"[STATUS] {message}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");
        }
    }

    public void Dispose() { }
}
