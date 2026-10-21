using FluentAssertions;
using SqlQueryStress.Models;

namespace SqlQueryStress.UnitTests.ServiceTests;

public class AuthenticationModeOptionTests
{
    [Fact]
    public void From_Integrated_ReturnsCorrectLabel()
    {
        var option = AuthenticationModeOption.From(AuthenticationMode.Integrated);

        option.Mode.Should().Be(AuthenticationMode.Integrated);
        option.Label.Should().Be("Integrated Authentication");
    }

    [Fact]
    public void From_SqlServer_ReturnsCorrectLabel()
    {
        var option = AuthenticationModeOption.From(AuthenticationMode.SqlServer);

        option.Mode.Should().Be(AuthenticationMode.SqlServer);
        option.Label.Should().Be("SQL Server Authentication");
    }

    [Fact]
    public void From_AzureMFA_ReturnsCorrectLabel()
    {
        var option = AuthenticationModeOption.From(AuthenticationMode.AzureMFA);

        option.Mode.Should().Be(AuthenticationMode.AzureMFA);
        option.Label.Should().Be("Azure Active Directory - Universal with MFA");
    }

    [Fact]
    public void From_InvalidEnum_ThrowsArgumentOutOfRangeException()
    {
        const AuthenticationMode invalidMode = (AuthenticationMode)999;

        Action action = () => AuthenticationModeOption.From(invalidMode);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}