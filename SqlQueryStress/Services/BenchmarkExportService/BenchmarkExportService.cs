using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SqlQueryStress.Services.BenchmarkExportService;

/// <summary>
/// Writes benchmark results to text and CSV files, or formats them for the
/// clipboard. Output mirrors the original SqlQueryStress WinForms app,
/// including the append-to-file behaviour for both formats.
/// </summary>
public sealed class BenchmarkExportService : IBenchmarkExportService
{
    private const string NotAvailable = "---";

    // The original WinForms app used StreamWriter(path, append) which writes UTF-8 without a BOM.
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private const string CsvHeader =
        "TestId,TestStartTime,ElapsedTime,Iterations,Threads,Delay,CompletedIterations,AvgCPUSeconds,AvgActualSeconds,AvgClientSeconds,AvgLogicalReads";

    /// <inheritdoc/>
    public string BuildText(BenchmarkResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var content = new StringBuilder();

        content.AppendLine($"Test ID: {result.TestId}");
        content.AppendLine($"Test TimeStamp: {result.TestStartTime.ToString(CultureInfo.CurrentCulture)}");
        content.AppendLine($"Elapsed Time: {FormatElapsed(result.Elapsed)}");
        content.AppendLine($"Number of Iterations: {result.NumIterations}");
        content.AppendLine($"Number of Threads: {result.NumThreads}");
        content.AppendLine($"Delay Between Queries (ms): {result.DelayBetweenQueries}");
        content.AppendLine($"CPU Seconds/Iteration (Avg): {FormatAverage(result.AverageCpuSeconds)}");
        content.AppendLine($"Actual Seconds/Iteration (Avg): {FormatAverage(result.AverageActualSeconds)}");
        content.AppendLine($"Iterations Completed: {result.CompletedIterations}");
        content.AppendLine($"Client Seconds/Iteration (Avg): {FormatAverage(result.AverageClientSeconds)}");
        content.AppendLine($"Logical Reads/Iteration (Avg): {FormatAverage(result.AverageLogicalReads)}");
        content.AppendLine();

        return content.ToString();
    }

    /// <inheritdoc/>
    public async Task ExportToTextAsync(string filePath, BenchmarkResult result)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        await using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write);
        await ExportToTextAsync(stream, result);
    }

    /// <inheritdoc/>
    public async Task ExportToTextAsync(Stream stream, BenchmarkResult result)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(result);

        await using var writer = new StreamWriter(stream, Utf8NoBom, leaveOpen: true);
        await writer.WriteAsync(BuildText(result));
        await writer.FlushAsync();
    }

    /// <inheritdoc/>
    public async Task ExportToCsvAsync(string filePath, BenchmarkResult result)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        var fileExists = File.Exists(filePath);

        await using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write);
        await ExportToCsvAsync(stream, result, fileExists);
    }

    /// <inheritdoc/>
    public Task ExportToCsvAsync(Stream stream, BenchmarkResult result)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(result);

        return ExportToCsvAsync(stream, result, stream.Length > 0);
    }

    private async Task ExportToCsvAsync(Stream stream, BenchmarkResult result, bool fileExists)
    {
        await using var writer = new StreamWriter(stream, Utf8NoBom, leaveOpen: true);

        // the header line is only written when appending to a brand-new file
        if (!fileExists)
            await writer.WriteLineAsync(CsvHeader);

        await writer.WriteLineAsync(BuildCsvRow(result));
        await writer.FlushAsync();
    }

    private static string BuildCsvRow(BenchmarkResult result)
    {
        return string.Join(",",
            result.TestId,
            result.TestStartTime.ToString(CultureInfo.CurrentCulture),
            FormatElapsed(result.Elapsed),
            result.NumIterations,
            result.NumThreads,
            result.DelayBetweenQueries,
            result.CompletedIterations,
            FormatAverage(result.AverageCpuSeconds),
            FormatAverage(result.AverageActualSeconds),
            FormatAverage(result.AverageClientSeconds),
            FormatAverage(result.AverageLogicalReads));
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        // Mirrors the original WinForms elapsed-text formatting, which kept
        // the first 13 characters of the default TimeSpan representation.
        var theTime = elapsed.ToString();

        return theTime.Length > 8
            ? theTime[..13]
            : theTime + ".0000";
    }

    private static string FormatAverage(double? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.0000", CultureInfo.CurrentCulture)
            : NotAvailable;
    }
}