using FluentAssertions;
using SqlQueryStress.Models;
using SqlQueryStress.Services.DbConnectionService;

namespace SqlQueryStress.UnitTests.ServiceTests;

public class ConnectionStringBuilderTests
{
    private static DbConnectionInfo BuildInfo(
        AuthenticationMode auth = AuthenticationMode.Integrated,
        string server = "localhost",
        string login = "sa",
        string password = "pass",
        string database = "",
        EncryptOption encrypt = EncryptOption.Optional,
        ApplicationIntent intent = ApplicationIntent.ReadWrite,
        bool trustCert = false,
        bool enablePooling = true,
        int maxPoolSize = 2,
        string additionalParams = "")
    {
        return new DbConnectionInfo
        {
            Server = server,
            Authentication = auth,
            Login = login,
            Password = password,
            Database = database,
            EncryptOption = encrypt,
            ApplicationIntent = intent,
            TrustServerCertificate = trustCert,
            EnablePooling = enablePooling,
            MaxPoolSize = maxPoolSize,
            AdditionalParameters = additionalParams
        };
    }

    #region Authentication modes

    [Fact]
    public void Build_IntegratedAuth_ContainsIntegratedSecurity()
    {
        var info = BuildInfo(auth: AuthenticationMode.Integrated);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Integrated Security=True");
        cs.Should().NotContain("User ID=");
    }

    [Fact]
    public void Build_SqlServerAuth_ContainsUserIdAndPassword()
    {
        var info = BuildInfo(auth: AuthenticationMode.SqlServer, login: "admin", password: "secret");
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("User ID=admin");
        cs.Should().Contain("Password=secret");
        cs.Should().NotContain("Integrated Security=True");
    }

    [Fact]
    public void Build_AzureMFA_ContainsActiveDirectoryInteractive()
    {
        var info = BuildInfo(auth: AuthenticationMode.AzureMFA, login: "user@contoso.com");
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Authentication=ActiveDirectoryInteractive");
        cs.Should().Contain("User ID=user@contoso.com");
    }

    #endregion

    #region EncryptOption variants

    [Fact]
    public void Build_EncryptOption_Optional_ContainsEncryptFalse()
    {
        var info = BuildInfo(encrypt: EncryptOption.Optional);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Encrypt=False");
    }

    [Fact]
    public void Build_EncryptOption_False_ContainsEncryptFalse()
    {
        var info = BuildInfo(encrypt: EncryptOption.False);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Encrypt=False");
    }

    [Fact]
    public void Build_EncryptOption_True_ContainsEncryptTrue()
    {
        var info = BuildInfo(encrypt: EncryptOption.True);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Encrypt=True");
    }

    [Fact]
    public void Build_EncryptOption_Strict_ContainsEncryptStrict()
    {
        var info = BuildInfo(encrypt: EncryptOption.Strict);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Encrypt=Strict");
    }

    #endregion

    #region ApplicationIntent

    [Fact]
    public void Build_ApplicationIntent_ReadWrite_ContainsReadWrite()
    {
        var info = BuildInfo(intent: ApplicationIntent.ReadWrite);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Application Intent=ReadWrite");
    }

    [Fact]
    public void Build_ApplicationIntent_ReadOnly_ContainsReadOnly()
    {
        var info = BuildInfo(intent: ApplicationIntent.ReadOnly);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Application Intent=ReadOnly");
    }

    #endregion

    #region Pooling

    [Fact]
    public void Build_PoolingEnabled_ContainsPoolSizes()
    {
        var info = BuildInfo(enablePooling: true, maxPoolSize: 5);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Max Pool Size=5");
        cs.Should().Contain("Min Pool Size=5");
    }

    [Fact]
    public void Build_PoolingDisabled_ContainsPoolingFalse()
    {
        var info = BuildInfo(enablePooling: false);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Pooling=False");
    }

    #endregion

    #region ConnectionTimeout

    [Fact]
    public void Build_ConnectionTimeout_GreaterThanZero_ContainsTimeout()
    {
        var info = BuildInfo();
        var cs = ConnectionStringBuilder.Build(info, 30);

        cs.Should().Contain("Connect Timeout=30");
    }

    [Fact]
    public void Build_ConnectionTimeout_Zero_DoesNotContainTimeout()
    {
        var info = BuildInfo();
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().NotContain("Connect Timeout=");
    }

    #endregion

    #region Database

    [Fact]
    public void Build_Database_NonEmpty_ContainsInitialCatalog()
    {
        var info = BuildInfo(database: "TestDB");
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Initial Catalog=TestDB");
    }

    [Fact]
    public void Build_Database_Empty_DoesNotContainInitialCatalog()
    {
        var info = BuildInfo(database: "");
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().NotContain("Initial Catalog=");
    }

    #endregion

    #region AdditionalParameters

    [Fact]
    public void Build_AdditionalParameters_AppendedWithSemicolon()
    {
        var info = BuildInfo(additionalParams: "ConnectRetryCount=3");
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().EndWith(";ConnectRetryCount=3");
    }

    [Fact]
    public void Build_AdditionalParameters_Empty_NoTrailingSemicolon()
    {
        var info = BuildInfo(additionalParams: "");
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().NotEndWith(";");
    }

    #endregion

    #region TrustServerCertificate

    [Fact]
    public void Build_TrustServerCertificate_True_ContainsTrustServerCertificate()
    {
        var info = BuildInfo(trustCert: true);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Trust Server Certificate=True");
    }

    [Fact]
    public void Build_TrustServerCertificate_False_ContainsTrustServerCertificateFalse()
    {
        var info = BuildInfo(trustCert: false);
        var cs = ConnectionStringBuilder.Build(info, 0);

        cs.Should().Contain("Trust Server Certificate=False");
    }

    #endregion
}
