using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using PdfToSpeechApp.Interfaces;
using PdfToSpeechApp.Services.Core;

namespace PdfToSpeechApp.Tests
{
    public class PdfToSpeechProcessorTests : IDisposable
    {
        private readonly Mock<IPdfParser> _parserMock;
        private readonly Mock<ITtsService> _ttsServiceMock;
        private readonly Mock<IAudioConverter> _audioConverterMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly Mock<IAppConfig> _configMock;
        private readonly PdfToSpeechProcessor _processor;
        private readonly string _testOutputDir;

        public PdfToSpeechProcessorTests()
        {
            _parserMock = new Mock<IPdfParser>();
            _ttsServiceMock = new Mock<ITtsService>();
            _audioConverterMock = new Mock<IAudioConverter>();
            _loggerMock = new Mock<ILogger>();
            _configMock = new Mock<IAppConfig>();

            _testOutputDir = Path.Combine(Path.GetTempPath(), "PdfToSpeechTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testOutputDir);

            _configMock.Setup(c => c.ResolvedModelPath).Returns("dummy_model.onnx");
            _configMock.Setup(c => c.OutputDir).Returns(_testOutputDir);

            // Setup logger mocks to actually invoke the operations
            _loggerMock.Setup(l => l.RunWithProgressAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<IProgress<int>, Task>>()))
                .Returns<string, int, Func<IProgress<int>, Task>>((desc, max, operation) =>
                {
                    var progress = new Progress<int>();
                    return operation(progress);
                });

            _loggerMock.Setup(l => l.RunWithStatusAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task>>()))
                .Returns<string, Func<Task>>((msg, operation) => operation());

            _processor = new PdfToSpeechProcessor(
                _parserMock.Object,
                _ttsServiceMock.Object,
                _audioConverterMock.Object,
                _loggerMock.Object,
                _configMock.Object
            );
        }

        public void Dispose()
        {
            if (Directory.Exists(_testOutputDir))
            {
                try { Directory.Delete(_testOutputDir, true); } catch { }
            }
        }

        [Fact]
        public async Task ProcessFileAsync_GeneratesAudioAndConvertsToMp3()
        {
            // Arrange
            string pdfPath = Path.Combine(_testOutputDir, "test.pdf");
            File.WriteAllText(pdfPath, "dummy content");

            var pages = new List<string> { "Page 1", "Page 2" };
            var parseResult = new PdfParseResult(2, pages);

            _parserMock.Setup(p => p.ExtractText(pdfPath)).Returns(parseResult);

            // Mock TTS to simulate output file creation
            _ttsServiceMock.Setup(t => t.GenerateAudioAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IProgress<int>>()))
                .Callback<IEnumerable<string>, string, string, IProgress<int>>((chunks, output, model, progress) =>
                {
                    // Simulate file creation
                    File.WriteAllText(output, "wav content");
                    // Report progress
                    progress?.Report(1);
                    progress?.Report(2);
                })
                .Returns(Task.CompletedTask);

            // Act
            await _processor.ProcessFileAsync(pdfPath);

            // Assert
            _parserMock.Verify(p => p.ExtractText(pdfPath), Times.Once);

            _ttsServiceMock.Verify(t => t.GenerateAudioAsync(
                It.IsAny<IEnumerable<string>>(),
                It.Is<string>(s => s.EndsWith(".wav")),
                "dummy_model.onnx",
                It.IsAny<IProgress<int>>()), Times.Once);

            _audioConverterMock.Verify(c => c.ConvertToMp3Async(
                It.Is<string>(s => s.EndsWith(".wav")),
                It.Is<string>(s => s.EndsWith(".mp3"))), Times.Once);

            // Verify logger interactions
            _loggerMock.Verify(l => l.LogHeader(It.Is<string>(s => s.Contains("test.pdf"))), Times.Once);
            _loggerMock.Verify(l => l.Log(It.Is<string>(s => s.Contains("pages"))), Times.Once);
            _loggerMock.Verify(l => l.RunWithProgressAsync(
                It.IsAny<string>(),
                2,
                It.IsAny<Func<IProgress<int>, Task>>()), Times.Once);
            _loggerMock.Verify(l => l.RunWithStatusAsync(
                It.Is<string>(s => s.Contains("MP3")),
                It.IsAny<Func<Task>>()), Times.Once);
            _loggerMock.Verify(l => l.LogSuccessPanel(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains(".mp3"))), Times.Once);
        }

        [Fact]
        public async Task ProcessFileAsync_HandlesMissingFileGracefully()
        {
            // Arrange
            string missingPath = Path.Combine(_testOutputDir, "missing.pdf");

            // Act
            await _processor.ProcessFileAsync(missingPath);

            // Assert
            _loggerMock.Verify(l => l.Log(It.Is<string>(s => s.Contains("Could not access file"))), Times.Once);
            _parserMock.Verify(p => p.ExtractText(It.IsAny<string>()), Times.Never);
        }
    }
}
