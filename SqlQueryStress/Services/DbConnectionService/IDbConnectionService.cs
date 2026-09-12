using SqlQueryStress.Models;

namespace SqlQueryStress.Services.DbConnectionService;

public interface IDbConnectionService
{
    DbConnectionInfo? MainDbConnectionInfo { get; }
    DbConnectionInfo? ParamDbConnectionInfo { get; }

    /// <summary>
    /// Whether the Parameterization database connection should be the same as the Main database connection
    /// </summary>
    bool ShareDbSettings { get; set; }

    void UpdateMainDbConnectionInfo(DbConnectionInfo dbConnectionInfo);
    void UpdateParamDbConnectionInfo(DbConnectionInfo dbConnectionInfo);
}