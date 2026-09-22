using System.Collections.Generic;
using System.Threading.Tasks;
using SqlQueryStress.Models;

namespace SqlQueryStress.Services.DbConnectionService;

public interface IDbConnectionService
{
    DbConnectionInfo MainDbConnectionInfo { get; }
    DbConnectionInfo ParamDbConnectionInfo { get; }

    /// <summary>
    /// Whether the Parameterization database connection should be the same as the Main database connection
    /// </summary>
    bool ShareDbSettings { get; set; }

    void UpdateMainDbConnectionInfo(DbConnectionInfo dbConnectionInfo);
    void UpdateParamDbConnectionInfo(DbConnectionInfo dbConnectionInfo);

    /// <summary>
    /// Tests whether a connections can be opened using the supplied db connection settings
    /// </summary>
    /// <param name="dbConnectionInfo">DB connection settings to test</param>
    /// <param name="connectionTimeout">Timeout in seconds</param>
    /// <returns>true if possible, false otherwise</returns>
    Task<bool> TestConnectionAsync(DbConnectionInfo dbConnectionInfo, int connectionTimeout);
    
    
    /// <summary>
    /// retreives available databases for the supplied SQL servewr connection
    /// </summary>
    /// <param name="dbConnectionInfo">Database connection settings</param>
    /// <param name="connectionTimeout">Timeout in seconds</param>
    /// <returns>List of available databases, empty if non exist</returns>
    Task<IReadOnlyList<string>> GetDatabaseListAsync(DbConnectionInfo dbConnectionInfo, int connectionTimeout);
}