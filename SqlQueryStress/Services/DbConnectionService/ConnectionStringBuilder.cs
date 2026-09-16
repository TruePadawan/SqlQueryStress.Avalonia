using System;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Models;

namespace SqlQueryStress.Services.DbConnectionService;

using SqlApplicationIntent = Microsoft.Data.SqlClient.ApplicationIntent;
using SqlEncryptOption = Microsoft.Data.SqlClient.SqlConnectionEncryptOption;

public static class ConnectionStringBuilder
{
    public static string Build(DbConnectionInfo info, int connectionTimeout)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = info.Server,
            ApplicationName = "SqlQueryStress",
            TrustServerCertificate = info.TrustServerCertificate,
        };

        switch (info.Authentication)
        {
            case AuthenticationMode.Integrated:
                builder.IntegratedSecurity = true;
                break;
            case AuthenticationMode.SqlServer:
                builder.UserID = info.Login;
                builder.Password = info.Password;
                break;
            case AuthenticationMode.AzureMFA:
                builder.UserID = info.Login;
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive;
                break;
        }

        if (info.Database.Length > 0)
            builder.InitialCatalog = info.Database;

        if (connectionTimeout > 0)
            builder.ConnectTimeout = connectionTimeout;

        if (info.MaxPoolSize > 0)
        {
            builder.MaxPoolSize = info.MaxPoolSize;
            builder.MinPoolSize = info.MaxPoolSize;
        }

        builder.Pooling = info.EnablePooling;

        builder.ApplicationIntent = info.ApplicationIntent switch
        {
            Models.ApplicationIntent.ReadWrite => SqlApplicationIntent.ReadWrite,
            Models.ApplicationIntent.ReadOnly => SqlApplicationIntent.ReadOnly,
            _ => throw new ArgumentOutOfRangeException() 
        };

        builder.Encrypt = info.EncryptOption switch
        {
            EncryptOption.Optional => SqlEncryptOption.Optional,
            EncryptOption.False => SqlEncryptOption.Optional,
            EncryptOption.True => SqlEncryptOption.Mandatory,
            EncryptOption.Strict => SqlEncryptOption.Strict,
            _ => throw new ArgumentOutOfRangeException()
        };

        string connectionString = builder.ConnectionString;
        if (!string.IsNullOrEmpty(info.AdditionalParameters))
            connectionString = $"{connectionString};{info.AdditionalParameters}";

        return connectionString;
    }
}