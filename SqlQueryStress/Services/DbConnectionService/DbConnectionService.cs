using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Models;

namespace SqlQueryStress.Services.DbConnectionService;

/// <summary>
/// Service that manages the database connections
/// </summary>
public class DbConnectionService : IDbConnectionService
{
    private const int AzureFirewallError = 40615;
    private const int LoginFailedError = 18456;
    private const int CannotOpenDatabaseError = 4060;

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
   
    /// <inheritdoc/>
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
        ConnectionSettingsHelper.AttachEventHandler(connection);

        try
        {
            await connection.OpenAsync();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to connect to server: {e}");
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetDatabaseListAsync(
        DbConnectionInfo dbConnectionInfo,
        int connectionTimeout
    )
    {
        var databases = new List<string>();
        var hasDatabase = !string.IsNullOrEmpty(dbConnectionInfo.Database);
        try
        {
            var connectionString = ConnectionStringBuilder.Build(dbConnectionInfo, connectionTimeout);

            await using var connection = new SqlConnection(connectionString);
            ConnectionSettingsHelper.AttachEventHandler(connection);
            await connection.OpenAsync();

            const string query = """
                                 SELECT databases.name
                                 FROM sys.databases
                                 WHERE databases.state = 0
                                 ORDER BY databases.name ASC
                                 """;

            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                databases.Add(reader.GetString(0));

            return databases;
        }
        catch (SqlException e) when (e.Number is AzureFirewallError or LoginFailedError)
        {
            return [];
        }
        catch (SqlException e) when (e.Number == CannotOpenDatabaseError && hasDatabase)
        {
            var serverOnlyConnection = dbConnectionInfo with
            {
                Database = ""
            };

            return await GetDatabaseListAsync(serverOnlyConnection, connectionTimeout);
        }
    }
}