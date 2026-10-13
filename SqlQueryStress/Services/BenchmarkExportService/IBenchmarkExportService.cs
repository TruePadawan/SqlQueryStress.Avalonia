using System.IO;
using System.Threading.Tasks;

namespace SqlQueryStress.Services.BenchmarkExportService;

public interface IBenchmarkExportService
{
    /// <summary>
    /// Formats a benchmark result as the plain-text benchmark block.
    /// </summary>
    string BuildText(BenchmarkResult result);

    /// <summary>
    /// Appends the benchmark block to the given file, creating it if needed.
    /// </summary>
    Task ExportToTextAsync(string filePath, BenchmarkResult result);

    /// <summary>
    /// Appends the benchmark block to the given stream.
    /// </summary>
    Task ExportToTextAsync(Stream stream, BenchmarkResult result);

    /// <summary>
    /// Appends a benchmark row to the given CSV file, writing the header for a new file.
    /// </summary>
    Task ExportToCsvAsync(string filePath, BenchmarkResult result);

    /// <summary>
    /// Appends a benchmark row to the given stream, writing the header when it is empty.
    /// </summary>
    Task ExportToCsvAsync(Stream stream, BenchmarkResult result);
}