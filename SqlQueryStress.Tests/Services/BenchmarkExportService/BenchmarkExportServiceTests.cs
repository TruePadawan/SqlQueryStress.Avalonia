using System.Globalization;
using FluentAssertions;
using BenchmarkResultModel = SqlQueryStress.Services.BenchmarkExportService.BenchmarkResult;
using BenchmarkExportService = SqlQueryStress.Services.BenchmarkExportService.BenchmarkExportService;

namespace SqlQueryStress.Tests.Services;

public class BenchmarkExportServiceTests
{
    private readonly BenchmarkExportService _service = new();

    private static readonly Guid TestId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateTime TestStartTime = new(2026, 1, 1, 12, 0, 0);

    public BenchmarkExportServiceTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    private static BenchmarkResultModel BuildResult(
        double? cpu = 0.75,
        double? actual = 1.5,
        double? reads = 123.4567)
    {
        return new BenchmarkResultModel
        {
            TestId = TestId,
            TestStartTime = TestStartTime,
            Elapsed = new TimeSpan(0, 1, 23, 45, 12),
            NumIterations = 10,
            NumThreads = 4,
            DelayBetweenQueries = 100,
            CompletedIterations = 97,
            AverageClientSeconds = 0.5,
            AverageCpuSeconds = cpu,
            AverageActualSeconds = actual,
            AverageLogicalReads = reads
        };
    }

    [Fact]
    public void BuildText_WithCompleteResults_RendersOriginalFormat()
    {
        var text = _service.BuildText(BuildResult());

        text.Should().Be(
            $"Test ID: {TestId}\r\n" +
            "Test TimeStamp: 01/01/2026 12:00:00\r\n" +
            "Elapsed Time: 01:23:45.0120\r\n" +
            "Number of Iterations: 10\r\n" +
            "Number of Threads: 4\r\n" +
            "Delay Between Queries (ms): 100\r\n" +
            "CPU Seconds/Iteration (Avg): 0.7500\r\n" +
            "Actual Seconds/Iteration (Avg): 1.5000\r\n" +
            "Iterations Completed: 97\r\n" +
            "Client Seconds/Iteration (Avg): 0.5000\r\n" +
            "Logical Reads/Iteration (Avg): 123.4567\r\n" +
            "\r\n");
    }

    [Fact]
    public void BuildText_WithoutStatisticMessages_ShowsDashes()
    {
        var text = _service.BuildText(BuildResult(cpu: null, actual: null, reads: null));

        text.Should().Contain("CPU Seconds/Iteration (Avg): ---\r\n");
        text.Should().Contain("Actual Seconds/Iteration (Avg): ---\r\n");
        text.Should().Contain("Logical Reads/Iteration (Avg): ---\r\n");
        text.Should().Contain("Client Seconds/Iteration (Avg): 0.5000\r\n");
    }

    [Fact]
    public void BuildText_ZeroElapsed_RendersPaddedTime()
    {
        var result = new BenchmarkResultModel
        {
            TestId = TestId,
            TestStartTime = TestStartTime,
            Elapsed = TimeSpan.Zero,
            NumIterations = 10,
            NumThreads = 4,
            DelayBetweenQueries = 100,
            CompletedIterations = 97,
            AverageClientSeconds = 0.5,
            AverageCpuSeconds = 0.75,
            AverageActualSeconds = 1.5,
            AverageLogicalReads = 123.4567
        };

        var text = _service.BuildText(result);

        text.Should().Contain("Elapsed Time: 00:00:00.0000\r\n");
    }

    [Fact]
    public async Task ExportToCsvAsync_NewFile_WritesHeaderThenRow()
    {
        var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        try
        {
            await _service.ExportToCsvAsync(file, BuildResult());

            var lines = await File.ReadAllLinesAsync(file);

            lines.Should().HaveCount(2);
            lines[0].Should().Be(
                "TestId,TestStartTime,ElapsedTime,Iterations,Threads,Delay,CompletedIterations,AvgCPUSeconds,AvgActualSeconds,AvgClientSeconds,AvgLogicalReads");
            lines[1].Should().Be(
                $"{TestId},01/01/2026 12:00:00,01:23:45.0120,10,4,100,97,0.7500,1.5000,0.5000,123.4567");
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task ExportToCsvAsync_NewFile_DoesNotWriteByteOrderMark()
    {
        var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        try
        {
            await _service.ExportToCsvAsync(file, BuildResult());

            var bytes = await File.ReadAllBytesAsync(file);

            var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            hasBom.Should().BeFalse();
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task ExportToCsvAsync_ExistingFile_AppendsWithoutHeader()
    {
        var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        try
        {
            await _service.ExportToCsvAsync(file, BuildResult());
            await _service.ExportToCsvAsync(file, BuildResult());

            var lines = await File.ReadAllLinesAsync(file);

            lines.Should().HaveCount(3);
            lines[0].Should().StartWith("TestId,");
            lines[1].Should().StartWith($"{TestId},");
            lines[2].Should().StartWith($"{TestId},");
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task ExportToTextAsync_AppendsToExistingFile()
    {
        var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

        try
        {
            await _service.ExportToTextAsync(file, BuildResult());
            await _service.ExportToTextAsync(file, BuildResult());

            var content = await File.ReadAllTextAsync(file);

            content.Split("\r\n").Count(line => line == "Test ID: " + TestId).Should().Be(2);
        }
        finally
        {
            File.Delete(file);
        }
    }
}