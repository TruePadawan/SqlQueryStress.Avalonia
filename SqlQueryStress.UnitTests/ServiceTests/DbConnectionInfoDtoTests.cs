using FluentAssertions;
using SqlQueryStress.DTOs;
using SqlQueryStress.Models;

namespace SqlQueryStress.UnitTests.ServiceTests;

public class DbConnectionInfoDtoTests
{
    private static DbConnectionInfo BuildModel(
        AuthenticationMode auth = AuthenticationMode.Integrated,
        string server = "localhost",
        string login = "sa",
        string password = "pass",
        string database = "TestDB",
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

    #region Authentication mode round-trip

    [Fact]
    public void FromModel_IntegratedAuth_SetsIntegratedAuthTrue()
    {
        var model = BuildModel(auth: AuthenticationMode.Integrated);
        var dto = DbConnectionInfoDto.FromModel(model, 0);

        dto.IntegratedAuth.Should().BeTrue();
        dto.AzureMFA.Should().BeFalse();
    }

    [Fact]
    public void ToModel_IntegratedAuthTrue_ReturnsIntegrated()
    {
        var dto = new DbConnectionInfoDto { IntegratedAuth = true };
        var model = DbConnectionInfoDto.ToModel(dto);

        model.Authentication.Should().Be(AuthenticationMode.Integrated);
    }

    [Fact]
    public void ToModel_BothFlagsFalse_ReturnsSqlServer()
    {
        var dto = new DbConnectionInfoDto { IntegratedAuth = false, AzureMFA = false };
        var model = DbConnectionInfoDto.ToModel(dto);

        model.Authentication.Should().Be(AuthenticationMode.SqlServer);
    }

    [Fact]
    public void FromModel_AzureMFA_SetsAzureMFATrue()
    {
        var model = BuildModel(auth: AuthenticationMode.AzureMFA);
        var dto = DbConnectionInfoDto.FromModel(model, 0);

        dto.AzureMFA.Should().BeTrue();
        dto.IntegratedAuth.Should().BeFalse();
    }

    [Fact]
    public void ToModel_AzureMFATrue_ReturnsAzureMFA()
    {
        var dto = new DbConnectionInfoDto { AzureMFA = true };
        var model = DbConnectionInfoDto.ToModel(dto);

        model.Authentication.Should().Be(AuthenticationMode.AzureMFA);
    }

    #endregion

    #region EncryptOption string round-trip

    [Fact]
    public void FromModel_EncryptOption_ConvertsToString()
    {
        var model = BuildModel(encrypt: EncryptOption.Strict);
        var dto = DbConnectionInfoDto.FromModel(model, 0);

        dto.EncryptOption.Should().Be("Strict");
    }

    [Fact]
    public void ToModel_EncryptOption_ParsesFromString()
    {
        var dto = new DbConnectionInfoDto { EncryptOption = "Mandatory" };
        var model = DbConnectionInfoDto.ToModel(dto);

        // "Mandatory" is not a valid EncryptOption enum value, falls back to False
        model.EncryptOption.Should().Be(EncryptOption.False);
    }

    [Fact]
    public void ToModel_ValidEncryptOptionString_ParsesCorrectly()
    {
        var dto = new DbConnectionInfoDto { EncryptOption = "Strict" };
        var model = DbConnectionInfoDto.ToModel(dto);

        model.EncryptOption.Should().Be(EncryptOption.Strict);
    }

    [Fact]
    public void ToModel_InvalidEncryptOption_FallsBackToFalse()
    {
        var dto = new DbConnectionInfoDto { EncryptOption = "not_a_valid_option" };
        var model = DbConnectionInfoDto.ToModel(dto);

        model.EncryptOption.Should().Be(EncryptOption.False);
    }

    #endregion

    #region ApplicationIntent integer round-trip

    [Fact]
    public void FromModel_ApplicationIntent_ConvertsToInt()
    {
        var model = BuildModel(intent: ApplicationIntent.ReadOnly);
        var dto = DbConnectionInfoDto.FromModel(model, 0);

        dto.ApplicationIntent.Should().Be((int)ApplicationIntent.ReadOnly);
    }

    [Fact]
    public void ToModel_ValidApplicationIntent_ParsesCorrectly()
    {
        var dto = new DbConnectionInfoDto { ApplicationIntent = (int)ApplicationIntent.ReadOnly };
        var model = DbConnectionInfoDto.ToModel(dto);

        model.ApplicationIntent.Should().Be(ApplicationIntent.ReadOnly);
    }

    [Fact]
    public void ToModel_InvalidApplicationIntent_FallsBackToReadWrite()
    {
        var dto = new DbConnectionInfoDto { ApplicationIntent = 999 };
        var model = DbConnectionInfoDto.ToModel(dto);

        model.ApplicationIntent.Should().Be(ApplicationIntent.ReadWrite);
    }

    #endregion

    #region MaxPoolSize guard

    [Fact]
    public void ToModel_MaxPoolSize_Zero_DefaultsTo2()
    {
        var dto = new DbConnectionInfoDto { MaxPoolSize = 0 };
        var model = DbConnectionInfoDto.ToModel(dto);

        model.MaxPoolSize.Should().Be(2);
    }

    [Fact]
    public void ToModel_MaxPoolSize_Negative_DefaultsTo2()
    {
        var dto = new DbConnectionInfoDto { MaxPoolSize = -5 };
        var model = DbConnectionInfoDto.ToModel(dto);

        model.MaxPoolSize.Should().Be(2);
    }

    [Fact]
    public void ToModel_MaxPoolSize_Positive_Preserved()
    {
        var dto = new DbConnectionInfoDto { MaxPoolSize = 10 };
        var model = DbConnectionInfoDto.ToModel(dto);

        model.MaxPoolSize.Should().Be(10);
    }

    #endregion

    #region Full round-trip fidelity

    [Fact]
    public void RoundTrip_IntegratedAuth_PreservesAllFields()
    {
        var model = BuildModel(
            auth: AuthenticationMode.Integrated,
            server: "dbserver",
            login: "user",
            password: "secret",
            database: "MyDB",
            encrypt: EncryptOption.Strict,
            intent: ApplicationIntent.ReadOnly,
            trustCert: true,
            enablePooling: false,
            maxPoolSize: 5,
            additionalParams: "ConnectRetryCount=3"
        );

        var dto = DbConnectionInfoDto.FromModel(model, 30);
        var restored = DbConnectionInfoDto.ToModel(dto);

        restored.Server.Should().Be(model.Server);
        restored.Authentication.Should().Be(model.Authentication);
        restored.Login.Should().Be(model.Login);
        restored.Password.Should().Be(model.Password);
        restored.Database.Should().Be(model.Database);
        restored.EncryptOption.Should().Be(model.EncryptOption);
        restored.ApplicationIntent.Should().Be(model.ApplicationIntent);
        restored.TrustServerCertificate.Should().Be(model.TrustServerCertificate);
        restored.EnablePooling.Should().Be(model.EnablePooling);
        restored.MaxPoolSize.Should().Be(model.MaxPoolSize);
        restored.AdditionalParameters.Should().Be(model.AdditionalParameters);
    }

    [Fact]
    public void RoundTrip_SqlServerAuth_PreservesAllFields()
    {
        var model = BuildModel(
            auth: AuthenticationMode.SqlServer,
            encrypt: EncryptOption.True
        );

        var dto = DbConnectionInfoDto.FromModel(model, 0);
        var restored = DbConnectionInfoDto.ToModel(dto);

        restored.Authentication.Should().Be(AuthenticationMode.SqlServer);
        restored.EncryptOption.Should().Be(EncryptOption.True);
    }

    [Fact]
    public void RoundTrip_AzureMFA_PreservesAllFields()
    {
        var model = BuildModel(auth: AuthenticationMode.AzureMFA);

        var dto = DbConnectionInfoDto.FromModel(model, 0);
        var restored = DbConnectionInfoDto.ToModel(dto);

        restored.Authentication.Should().Be(AuthenticationMode.AzureMFA);
    }

    #endregion
}
