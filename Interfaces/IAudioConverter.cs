using System.Threading.Tasks;

namespace PdfToSpeechApp.Interfaces;

public interface IAudioConverter
{
    Task ConvertToMp3Async(string inputWav, string outputMp3);
}
