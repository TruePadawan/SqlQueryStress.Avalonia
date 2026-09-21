using System;
using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;

namespace SqlQueryStress.Services.DbConnectionService;

internal static class ConnectionSettingsHelper
{
    public static void AttachEventHandler(SqlConnection connection)
    {
        connection.StateChange += OnStateChange;
    }

    private static void OnStateChange(object? sender, StateChangeEventArgs args)
    {
        if (
            args.OriginalState != ConnectionState.Closed ||
            args.CurrentState != ConnectionState.Open
        )
            return;

        var settingsToApply = QuerySettings.Value;

        if (string.IsNullOrEmpty(settingsToApply))
            return;

        if (sender is not SqlConnection connection)
            return;

        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = settingsToApply;
        command.ExecuteNonQuery();
    }

    private static readonly Lazy<string> QuerySettings = new(LoadQuerySettings);

    private const string DefaultSettings =
        """
        -- Default SQL Server Management Studio query execution settings
        -- These settings match the default SSMS configuration for query execution
        -- ANSI settings
        SET QUOTED_IDENTIFIER ON;
        SET ANSI_NULL_DFLT_ON ON;
        SET ANSI_PADDING ON;
        SET ANSI_WARNINGS ON;
        SET ANSI_NULLS ON;
        
        -- Advanced settings
        SET ARITHABORT ON;
        SET CONCAT_NULL_YIELDS_NULL ON;
        
        -- Additional settings (default OFF in SSMS)
        SET IMPLICIT_TRANSACTIONS OFF;
        SET CURSOR_CLOSE_ON_COMMIT OFF;
        BEGIN TRY
        EXEC('SET XACT_ABORT OFF');
        END TRY
        BEGIN CATCH
        PRINT 'Cannot set XACT_ABORT OFF: ' + ERROR_MESSAGE();
        END CATCH;
        """;


    private static string LoadQuerySettings()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
            return DefaultSettings;

        var appPath = Path.GetDirectoryName(processPath);
        if (string.IsNullOrEmpty(appPath))
            return DefaultSettings;

        var settingsFile = Path.Combine(appPath, "querysettings.sql");
        return File.Exists(settingsFile)
            ? File.ReadAllText(settingsFile)
            : DefaultSettings;
    }
}