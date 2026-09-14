using System.IO;
using System.Threading.Tasks;

namespace SqlQueryStress.Services.SettingsFileService;

public interface ISettingsFileService
{
    Task SaveAsync(string filePath);
    Task LoadAsync(string filePath);
    Task SaveAsync(Stream stream);
    Task LoadAsync(Stream stream);
}
