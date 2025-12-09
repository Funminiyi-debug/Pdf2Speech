using System;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Infrastructure;

public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    public void LogError(string message, Exception? ex = null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERROR: {message}");
        if (ex != null)
        {
            Console.WriteLine(ex.ToString());
        }
        Console.ResetColor();
    }
}
