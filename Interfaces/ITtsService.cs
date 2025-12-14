using System.Collections.Generic;
using System.Threading.Tasks;

namespace PdfToSpeechApp.Interfaces;

public interface ITtsService
{
    Task GenerateAudioAsync(IEnumerable<string> textChunks, string outputPath, string modelPath, IProgress<int>? progress = null);
}
