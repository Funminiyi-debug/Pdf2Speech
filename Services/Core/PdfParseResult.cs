using System.Collections.Generic;

namespace PdfToSpeechApp.Services.Core;

public class PdfParseResult(
        int totalPages,
        IEnumerable<string> pages
    )
{
    public int TotalPages { get; } = totalPages;
    public IEnumerable<string> Pages { get; } = pages;
}
