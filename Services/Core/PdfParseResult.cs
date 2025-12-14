using System.Collections.Generic;

namespace PdfToSpeechApp.Services.Core;

public record PdfParseResult(
        int TotalPages,
        IEnumerable<string> Pages
    )
{
    public int TotalPages { get; } = TotalPages;
    public IEnumerable<string> Pages { get; } = Pages;
}
