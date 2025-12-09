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

    public IEnumerable<string> ExtractText(string filePath)
    {
        PdfDocument? document = null;
        try
        {
            document = PdfDocument.Open(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error opening PDF: {ex.Message}", ex);
            throw;
        }

        using (document)
        {
            foreach (var page in document.GetPages())
            {
                yield return page.Text;
            }
        }
    }
}
