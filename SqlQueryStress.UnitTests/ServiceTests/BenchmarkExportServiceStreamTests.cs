using System.Globalization;
using System.Text;
using FluentAssertions;
using SqlQueryStress.Services.BenchmarkExportService;
using BenchmarkResultModel = SqlQueryStress.Services.BenchmarkExportService.BenchmarkResult;

namespace SqlQueryStress.UnitTests.ServiceTests;

public class BenchmarkExportServiceStreamTests
{
    private readonly BenchmarkExportService _service = new();

    private static readonly Guid TestId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateTime TestStartTime = new(2026, 1, 1, 12, 0, 0);

    public BenchmarkExportServiceStreamTests()
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

    #region ExportToTextAsync(Stream)

    [Fact]
    public async Task ExportToTextAsync_Stream_WritesTextContent()
    {
        using var stream = new MemoryStream();
        await _service.ExportToTextAsync(stream, BuildResult());

        var content = Encoding.UTF8.GetString(stream.ToArray());

        content.Should().Contain($"Test ID: {TestId}");
        content.Should().Contain("Elapsed Time: 01:23:45.0120");
    }

    [Fact]
    public async Task ExportToTextAsync_Stream_DoesNotWriteBOM()
    {
        using var stream = new MemoryStream();
        await _service.ExportToTextAsync(stream, BuildResult());

        var bytes = stream.ToArray();

        var hasBom = bytes is [0xEF, 0xBB, 0xBF, ..];
        hasBom.Should().BeFalse();
    }

    #endregion

    #region ExportToCsvAsync(Stream)

    [Fact]
    public async Task ExportToCsvAsync_EmptyStream_WritesHeaderAndRow()
    {
        using var stream = new MemoryStream();
        await _service.ExportToCsvAsync(stream, BuildResult());

        var content = Encoding.UTF8.GetString(stream.ToArray());
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCountGreaterThanOrEqualTo(2);
        lines[0].Should().StartWith("TestId,");
        lines[1].Should().StartWith($"{TestId},");
    }

    [Fact]
    public async Task ExportToCsvAsync_NonEmptyStream_WritesRowOnly()
    {
        using var stream = new MemoryStream();

        // Pre-populate the stream so it's non-empty
        await stream.WriteAsync("existing,data\n"u8.ToArray());

        await _service.ExportToCsvAsync(stream, BuildResult());

        var content = Encoding.UTF8.GetString(stream.ToArray());
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // First line is the pre-existing data, second is the new row (no header)
        lines[0].Should().Be("existing,data");
        lines[1].Should().StartWith($"{TestId},");
        lines[1].Should().NotStartWith("TestId,");
    }

    #endregion

    #region FormatElapsed edge cases

    [Fact]
    public void FormatElapsed_ZeroTimeSpan_PaddedWithZeros()
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

        text.Should().Contain("Elapsed Time: 00:00:00.0000");
    }

    [Fact]
    public void FormatElapsed_ShortTimeSpan_LessThan8Chars_Padded()
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

        // "00:00:00" is 8 chars, padded to "00:00:00.0000"
        text.Should().Contain("Elapsed Time: 00:00:00.0000");
    }

    [Fact]
    public void FormatElapsed_LongTimeSpan_TruncatedTo13Chars()
    {
        // A TimeSpan with days produces a long string
        var result = new BenchmarkResultModel
        {
            TestId = TestId,
            TestStartTime = TestStartTime,
            Elapsed = new TimeSpan(30, 1, 23, 45, 123),
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

        // "30.01:23:45.1230000" is > 8 chars, truncated to 13 chars: "30.01:23:45"
        text.Should().Contain("Elapsed Time: 30.01:23:45");
    }

    #endregion
}