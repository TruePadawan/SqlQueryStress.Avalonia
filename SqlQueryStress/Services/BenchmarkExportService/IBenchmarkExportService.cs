using System.IO;
using System.Threading.Tasks;

namespace SqlQueryStress.Services.BenchmarkExportService;

public interface IBenchmarkExportService
{
    string BuildText(BenchmarkResult result);

    Task ExportToTextAsync(string filePath, BenchmarkResult result);
    Task ExportToTextAsync(Stream stream, BenchmarkResult result);

    Task ExportToCsvAsync(string filePath, BenchmarkResult result);
    Task ExportToCsvAsync(Stream stream, BenchmarkResult result);
}