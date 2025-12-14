using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using CliWrap;

namespace PdfToSpeechApp.IntegrationTests
{
    public class PdfToSpeechIntegrationTests : IAsyncLifetime
    {
        private IContainer? _container;
        private readonly string _solutionRoot;

        public PdfToSpeechIntegrationTests()
        {
            // Resolve solution root robustly by walking up from bin dir and current dir
            var binDir = AppDomain.CurrentDomain.BaseDirectory;

            string? TryFindRoot(string start)
            {
                var dir = new DirectoryInfo(start);
                for (int i = 0; i < 8 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, "PdfToSpeechApp.csproj");
                    if (File.Exists(candidate)) return dir.FullName;
                    dir = dir.Parent;
                }
                return null;
            }

            _solutionRoot =
                TryFindRoot(binDir)
                ?? TryFindRoot(Directory.GetCurrentDirectory())
                ?? throw new InvalidOperationException("Could not locate solution root containing PdfToSpeechApp.csproj");
        }

        public async Task InitializeAsync()
        {
            var imageName = "pdftospeech-integration";
            var dockerfilePath = "PdfToSpeechApp.IntegrationTests/Dockerfile.integration";

            // Build image manually
            // docker build -f [file] -t [tag] [context]
            var stdout = new System.Text.StringBuilder();
            var stderr = new System.Text.StringBuilder();

            var dockerfileAbs = Path.Combine(_solutionRoot, dockerfilePath);
            var contextAbs = _solutionRoot;

            var buildResult = await Cli.Wrap("docker")
                .WithArguments(args => args
                    .Add("build")
                    .Add("--progress=plain")
                    .Add("-f")
                    .Add(dockerfileAbs)
                    .Add("-t")
                    .Add(imageName)
                    .Add(contextAbs)) // Context
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderr))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            if (buildResult.ExitCode != 0)
            {
                throw new Exception($"Docker build failed. code={buildResult.ExitCode}\nDockerfile: {dockerfileAbs}\nContext: {contextAbs}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
            }

            _container = new ContainerBuilder()
                .WithImage(imageName)
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();
        }

        public async Task DisposeAsync()
        {
            if (_container != null)
            {
                await _container.DisposeAsync();
            }
        }

        [Fact]
        public async Task RunApp_GeneratingSampleAndProcessing_CreatesMp3()
        {
            // Arrange
            // The container works is already running.
            // We need to run the app to process the sample.
            // The sample is at /app/out/input/sample.pdf
            // App is at /app/out/PdfToSpeechApp.dll

            // Act
            // Run the app. It monitors the folder.
            // But the app is a long-running service (DirectoryFileMonitor)?
            // Or does unit mode exist? 
            // The app runs `app.RunAsync(args)`.
            // By default it starts monitoring.
            // We need it to process existing files and then exit?
            // "DirectoryFileMonitor" usually starts and waits.

            // Check Program.cs logic.
            // It runs `app.RunAsync`.
            // `DirectoryFileMonitor` uses `FileSystemWatcher`.
            // It also scans existing files?
            // If checking existing files, it processes them.
            // But the app loop waits for 'q' to quit.

            // Issue: The app is interactive/long-running.
            // Workaround: We can't easily wait for 'q' via ExecAsync unless we pipe input.
            // Better: Modify app to support a "--one-shot" or "--process <file>" flag?
            // User requested "Uses actual resources". 
            // I'll try to run it in background inside container, wait a bit, then check file?

            // Command: dotnet /app/out/PdfToSpeechApp.dll & sleep 10 && ls -l /app/out/output/sample.mp3

            // Command: Process the specific file. This should exit automatically.
            var execResult = await _container!.ExecAsync(new[]
            {
                "dotnet",
                "/app/out/PdfToSpeechApp.dll",
                "--process-file",
                "/app/out/input/sample.pdf"
            });

            // Check if parsing/processing logs are present
            // Note: ExecAsync captures stdout/stderr.

            // Verify file existence using exit code only (0 when file exists, non-zero otherwise)
            var fileCheck = await _container.ExecAsync(new[]
            {
                "/bin/sh", "-c", "[ -f /app/out/output/sample.mp3 ]"
            });

            // Assert
            Assert.Equal(0, execResult.ExitCode);
            Assert.Equal(0, fileCheck.ExitCode);
        }
    }
}
