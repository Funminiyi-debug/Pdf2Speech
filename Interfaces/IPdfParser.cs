using System.Collections.Generic;

namespace PdfToSpeechApp.Interfaces;

public interface IPdfParser
{
    IEnumerable<string> ExtractText(string filePath);
}
