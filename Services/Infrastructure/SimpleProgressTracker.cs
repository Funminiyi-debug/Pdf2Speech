using System;
using PdfToSpeechApp.Interfaces;
using Spectre.Console;

namespace PdfToSpeechApp.Services.Infrastructure;

/// <summary>
/// Simple progress tracker implementation for synchronous progress reporting.
/// </summary>
internal sealed class SimpleProgressTracker : IProgressTracker
{
    private readonly string _description;
    private readonly int _maxValue;
    private readonly bool _isTestMode;
    private int _currentValue;

    public SimpleProgressTracker(string description, int maxValue, bool isTestMode)
    {
        _description = description;
        _maxValue = maxValue;
        _isTestMode = isTestMode;
    }

    public void Report(int value)
    {
        _currentValue = value;
        if (_isTestMode)
        {
            double percent = (double)value / _maxValue;
            Console.WriteLine($"{percent:P0} ({value}/{_maxValue})");
        }
        else
        {
            double percent = (double)value / _maxValue;
            AnsiConsole.MarkupLine($"[cyan]{Markup.Escape(_description)}[/]: [green]{percent:P0}[/]");
        }
    }

    public void Complete()
    {
        _currentValue = _maxValue;
    }

    public void Dispose() { }
}
