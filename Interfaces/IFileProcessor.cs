using System.Threading.Tasks;

namespace PdfToSpeechApp.Interfaces;

public interface IFileProcessor
{
    Task ProcessFileAsync(string filePath);
}
