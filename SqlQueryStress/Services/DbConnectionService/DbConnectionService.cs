using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
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

    public async Task<bool> TestConnectionAsync(
        DbConnectionInfo dbConnectionInfo,
        int connectionTimeout
    )
    {
        var hasServer = !string.IsNullOrEmpty(dbConnectionInfo.Server);
        var usesSqlServerAuthentication = dbConnectionInfo.Authentication == AuthenticationMode.SqlServer;
        var hasSqlServerCredentials = !string.IsNullOrEmpty(dbConnectionInfo.Login) &&
                                      !string.IsNullOrEmpty(dbConnectionInfo.Password);
        
        if (!hasServer)
            return false;

        if (usesSqlServerAuthentication && !hasSqlServerCredentials)
            return false;
        
        var connectionString = ConnectionStringBuilder.Build(dbConnectionInfo, connectionTimeout);
        
        using var connection = new SqlConnection(connectionString);

        try
        {
            await connection.OpenAsync();
            return true;
        }
        catch(Exception e)
        {
            Console.WriteLine($"Failed to connect to server: {e}");
            return false;
        }
    }
}