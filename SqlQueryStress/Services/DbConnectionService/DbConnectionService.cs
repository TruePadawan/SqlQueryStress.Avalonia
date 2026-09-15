using SqlQueryStress.Models;

namespace SqlQueryStress.Services.DbConnectionService;

/// <summary>
/// Service that manages the database connections
/// </summary>
public class DbConnectionService : IDbConnectionService
{
    public DbConnectionInfo MainDbConnectionInfo { get; private set; } = new()
    {
        Server = "(local)",
        Authentication = AuthenticationMode.SqlServer,
        Database = ""
    };

    public DbConnectionInfo ParamDbConnectionInfo { get; private set; } = new()
    {
        Server = "(local)",
        Authentication = AuthenticationMode.SqlServer,
        Database = ""
    };

    public bool ShareDbSettings { get; set; }

    public void UpdateMainDbConnectionInfo(DbConnectionInfo dbConnectionInfo)
    {
        MainDbConnectionInfo = dbConnectionInfo;
    }

    public void UpdateParamDbConnectionInfo(DbConnectionInfo dbConnectionInfo)
    {
        ParamDbConnectionInfo = dbConnectionInfo;
    }
}