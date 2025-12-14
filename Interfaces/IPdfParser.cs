using PdfToSpeechApp.Services.Core;

namespace PdfToSpeechApp.Interfaces;

public interface IPdfParser
{
    PdfParseResult ExtractText(string filePath);
}
