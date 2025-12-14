using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PdfToSpeechApp.Interfaces;
using Spectre.Console;

namespace PdfToSpeechApp.Services.Infrastructure;

/// <summary>
/// Rule for formatting log messages based on content patterns.
/// </summary>
public class MessageFormattingRule
{
    public Func<string, bool> Matches { get; init; } = _ => false;
    public string Color { get; init; } = "white";
}

public class ConsoleLogger : ILogger
{
    // Check if we're in test mode (captured writer or environment variable)
    private bool IsTestMode =>
        Console.Out is StringWriter ||
        Console.Out.GetType().Name.Contains("StringWriter", StringComparison.Ordinal) ||
        Console.IsOutputRedirected ||
        Environment.GetEnvironmentVariable("PDF_TO_SPEECH_TEST_MODE") == "1";

    // Extensible list of formatting rules - add new rules without modifying Log method
    private readonly List<MessageFormattingRule> _formattingRules;

    public ConsoleLogger() : this(GetDefaultFormattingRules())
    {
    }

    public ConsoleLogger(IEnumerable<MessageFormattingRule> formattingRules)
    {
        _formattingRules = new List<MessageFormattingRule>(formattingRules);
    }

    /// <summary>
    /// Gets the default formatting rules. Override or extend to add new patterns.
    /// </summary>
    public static List<MessageFormattingRule> GetDefaultFormattingRules() => new()
    {
        new() { Matches = m => m.Contains("[Piper]", StringComparison.OrdinalIgnoreCase), Color = "cyan" },
        new() { Matches = m => m.Contains("[FFmpeg]", StringComparison.OrdinalIgnoreCase), Color = "magenta" },
        new() { Matches = m => m.Contains("Success", StringComparison.OrdinalIgnoreCase) ||
                               m.Contains("completed", StringComparison.OrdinalIgnoreCase), Color = "green" },
        new() { Matches = m => m.Contains("Processing", StringComparison.OrdinalIgnoreCase) ||
                               m.Contains("Starting", StringComparison.OrdinalIgnoreCase), Color = "blue" },
        new() { Matches = m => m.Contains("Downloading", StringComparison.OrdinalIgnoreCase), Color = "yellow" },
        new() { Matches = m => m.Contains("Warning", StringComparison.OrdinalIgnoreCase), Color = "yellow" },
    };

    /// <summary>
    /// Adds a new formatting rule to the logger.
    /// </summary>
    public void AddFormattingRule(MessageFormattingRule rule)
    {
        _formattingRules.Add(rule);
    }

    public void Log(string message)
    {
        if (IsTestMode)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            return;
        }

        var timestamp = $"[grey][[{DateTime.Now:HH:mm:ss}]][/]";
        var color = GetColorForMessage(message);
        AnsiConsole.MarkupLine($"{timestamp} [{color}]{EscapeMarkup(message)}[/]");
    }

    private string GetColorForMessage(string message)
    {
        foreach (var rule in _formattingRules)
        {
            if (rule.Matches(message))
            {
                return rule.Color;
            }
        }
        return "white"; // Default color
    }

    public void LogError(string message, Exception? ex = null)
    {
        if (IsTestMode)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERROR: {message}");
            if (ex != null) Console.WriteLine(ex.ToString());
            return;
        }

        var timestamp = $"[grey][[{DateTime.Now:HH:mm:ss}]][/]";
        AnsiConsole.MarkupLine($"{timestamp} [red bold]ERROR:[/] [red]{EscapeMarkup(message)}[/]");

        if (ex != null)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }

    public void LogSuccess(string message)
    {
        if (IsTestMode)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] SUCCESS: {message}");
            return;
        }

        var timestamp = $"[grey][[{DateTime.Now:HH:mm:ss}]][/]";
        AnsiConsole.MarkupLine($"{timestamp} [green bold]✓[/] [green]{EscapeMarkup(message)}[/]");
    }

    public void LogWarning(string message)
    {
        if (IsTestMode)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] WARNING: {message}");
            return;
        }

        var timestamp = $"[grey][[{DateTime.Now:HH:mm:ss}]][/]";
        AnsiConsole.MarkupLine($"{timestamp} [yellow bold]⚠[/] [yellow]{EscapeMarkup(message)}[/]");
    }

    public void LogHeader(string title)
    {
        if (IsTestMode)
        {
            Console.WriteLine($"=== {title} ===");
            return;
        }

        AnsiConsole.Write(new Rule($"[blue bold]{EscapeMarkup(title)}[/]").RuleStyle("grey"));
    }

    public void LogSuccessPanel(string title, string message)
    {
        if (IsTestMode)
        {
            Console.WriteLine($"[SUCCESS] {title}: {message}");
            return;
        }

        var panel = new Panel($"[green]{EscapeMarkup(message)}[/]")
            .Header($"[green bold]✓ {EscapeMarkup(title)}[/]")
            .HeaderAlignment(Justify.Left)
            .BorderColor(Color.Green)
            .RoundedBorder();

        AnsiConsole.Write(panel);
    }

    public IProgressTracker CreateProgress(string description, int maxValue)
    {
        return new SimpleProgressTracker(description, maxValue, IsTestMode);
    }

    public IStatusContext CreateStatus(string message)
    {
        return new SimpleStatusContext(message, IsTestMode);
    }

    public async Task RunWithProgressAsync(string description, int maxValue, Func<IProgress<int>, Task> operation)
    {
        if (IsTestMode)
        {
            var progress = new Progress<int>(current =>
            {
                double percent = (double)current / maxValue;
                Console.WriteLine($"{percent:P0} ({current}/{maxValue})");
            });
            await operation(progress);
            return;
        }

        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn()
                {
                    CompletedStyle = new Style(foreground: Color.Green),
                    FinishedStyle = new Style(foreground: Color.Green),
                    RemainingStyle = new Style(foreground: Color.Grey)
                },
                new PercentageColumn(),
                new SpinnerColumn(Spinner.Known.Dots)
                {
                    Style = new Style(foreground: Color.Cyan1)
                },
                new ElapsedTimeColumn()
            })
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[cyan]{EscapeMarkup(description)}[/]", maxValue: maxValue);

                var progress = new Progress<int>(current =>
                {
                    task.Value = current;
                });

                await operation(progress);
                task.StopTask();
            });

        AnsiConsole.WriteLine();
    }

    public async Task RunWithStatusAsync(string message, Func<Task> operation)
    {
        if (IsTestMode)
        {
            Console.WriteLine($"[STATUS] {message}");
            await operation();
            return;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync($"[yellow]{EscapeMarkup(message)}[/]", async ctx =>
            {
                await operation();
            });
    }

    public async Task RunDownloadProgressAsync(string[] taskNames, Func<Action<int, long, long>, Task> operation)
    {
        if (IsTestMode)
        {
            foreach (var name in taskNames)
            {
                Console.WriteLine($"[DOWNLOAD] {name}");
            }
            await operation((index, current, total) =>
            {
                if (total > 0)
                {
                    double percent = (double)current / total;
                    Console.WriteLine($"{taskNames[index]}: {percent:P0}");
                }
            });
            return;
        }

        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn()
                {
                    CompletedStyle = new Style(foreground: Color.Yellow),
                    FinishedStyle = new Style(foreground: Color.Green),
                    RemainingStyle = new Style(foreground: Color.Grey)
                },
                new PercentageColumn(),
                new SpinnerColumn(Spinner.Known.Dots)
                {
                    Style = new Style(foreground: Color.Yellow)
                },
                new DownloadedColumn(),
                new TransferSpeedColumn()
            })
            .StartAsync(async ctx =>
            {
                var tasks = new ProgressTask[taskNames.Length];
                for (int i = 0; i < taskNames.Length; i++)
                {
                    tasks[i] = ctx.AddTask($"[cyan]{EscapeMarkup(taskNames[i])}[/]");
                }

                await operation((index, current, total) =>
                {
                    if (index >= 0 && index < tasks.Length)
                    {
                        tasks[index].MaxValue = total > 0 ? total : 100;
                        tasks[index].Value = current;
                        if (current >= total && total > 0)
                        {
                            tasks[index].StopTask();
                        }
                    }
                });
            });
    }

    private static string EscapeMarkup(string text)
    {
        return Markup.Escape(text);
    }
}
