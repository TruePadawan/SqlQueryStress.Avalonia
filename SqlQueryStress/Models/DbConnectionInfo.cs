using System;

namespace SqlQueryStress.Models;

/// <summary>
/// Represents a connection to a database
/// </summary>
public record DbConnectionInfo
{
    public required string Server { get; init; }
    public required AuthenticationMode Authentication { get; init; }
    public string Login { get; init; } = "";
    public string Password { get; init; } = "";
    public required string Database { get; init; }
    public ApplicationIntent ApplicationIntent { get; init; } = ApplicationIntent.ReadWrite;
    public EncryptOption EncryptOption { get; init; } = EncryptOption.False;
    public bool TrustServerCertificate { get; init; } = false;
    public string AdditionalParameters { get; init; } = "";
    public bool EnablePooling { get; init; } = true;
    public int MaxPoolSize { get; init; } = 2;
}

public enum AuthenticationMode
{
    Integrated,
    SqlServer,
    AzureMFA
}

public record AuthenticationModeOption(AuthenticationMode Mode, string Label)
{
    public static AuthenticationModeOption From(AuthenticationMode mode)
    {
        return mode switch
        {
            AuthenticationMode.Integrated => new AuthenticationModeOption(mode, "Integrated Authentication"),
            AuthenticationMode.SqlServer => new AuthenticationModeOption(mode, "SQL Server Authentication"),
            AuthenticationMode.AzureMFA => new AuthenticationModeOption(mode,
                "Azure Active Directory - Universal with MFA"),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
};

public enum ApplicationIntent
{
    ReadWrite,
    ReadOnly
}

public enum EncryptOption
{
    False,
    True,
    Strict
}