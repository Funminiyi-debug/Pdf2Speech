using System;
using System.Collections.Generic;
using UglyToad.PdfPig;
using PdfToSpeechApp.Interfaces;

namespace PdfToSpeechApp.Services.Core;

public class PdfPigParser : IPdfParser
{
    private readonly ILogger _logger;

    public PdfPigParser(ILogger logger)
    {
        _logger = logger;
    }

    public PdfParseResult ExtractText(string filePath)
    {
        PdfDocument document;
        try
        {
            document = PdfDocument.Open(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error opening PDF: {ex.Message}", ex);
            throw;
        }

        // We transfer ownership of the document to the iterator
        int count = document.NumberOfPages;
        return new PdfParseResult(count, EnumeratePages(document));
    }

    private IEnumerable<string> EnumeratePages(PdfDocument document)
    {
        using (document)
        {
            foreach (var page in document.GetPages())
            {
                yield return page.Text;
            }
        }
    }
}
