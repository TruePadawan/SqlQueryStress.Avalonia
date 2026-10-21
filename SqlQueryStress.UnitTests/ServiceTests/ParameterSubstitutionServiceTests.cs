using FluentAssertions;
using SqlQueryStress.Services.ParameterSubstitutionService;

namespace SqlQueryStress.UnitTests.ServiceTests;

public class ParameterSubstitutionServiceTests
{
    private readonly ParameterSubstitutionService _service = new();

    [Fact]
    public void ExtractParametersFromQuery_SingleParameter_ReturnsOneParameter()
    {
        const string query = "SELECT * FROM T WHERE id = @id";
        var parameters = _service.ExtractParametersFromQuery(query);

        parameters.Should().Equal("@id");
    }

    [Fact]
    public void ExtractParametersFromQuery_MultipleParameters_ReturnsAllParameters()
    {
        const string query = "SELECT * FROM T WHERE id = @id AND name = @name";
        var parameters = _service.ExtractParametersFromQuery(query);

        parameters.Should().Equal("@id", "@name");
    }

    [Fact]
    public void ExtractParametersFromQuery_DuplicateParameters_ReturnsUnique()
    {
        const string query = "SELECT * FROM T WHERE id = @id OR name = @id";
        var parameters = _service.ExtractParametersFromQuery(query);

        parameters.Should().Equal("@id");
    }

    [Fact]
    public void ExtractParametersFromQuery_CaseInsensitive_Deduplicates()
    {
        const string query = "SELECT * FROM T WHERE id = @Id OR name = @id";
        var parameters = _service.ExtractParametersFromQuery(query);

        parameters.Should().Equal("@id");
    }

    [Fact]
    public void ExtractParametersFromQuery_EmptyQuery_ReturnsEmpty()
    {
        const string query = "";
        var parameters = _service.ExtractParametersFromQuery(query);

        parameters.Should().BeEmpty();
    }

    [Fact]
    public void ExtractParametersFromQuery_NoParameters_ReturnsEmpty()
    {
        const string query = "SELECT * FROM T WHERE id = 1";
        var parameters = _service.ExtractParametersFromQuery(query);

        parameters.Should().BeEmpty();
    }

    [Fact]
    public void ExtractParametersFromQuery_ParameterInParentheses_ReturnsParameter()
    {
        const string query = "SELECT * FROM T WHERE id IN (@id1, @id2)";
        var parameters = _service.ExtractParametersFromQuery(query);

        parameters.Should().Equal("@id1", "@id2");
    }

    [Fact]
    public void ExtractParametersFromQuery_ParameterAfterComma_ReturnsParameter()
    {
        const string query = "EXEC sp_who @spid, @status";
        var parameters = _service.ExtractParametersFromQuery(query);

        parameters.Should().Equal("@spid", "@status");
    }

    [Fact]
    public void ExtractParametersFromQuery_ParameterAfterWhitespace_ReturnsParameter()
    {
        const string query = "EXEC sp_who @spid @status";
        var parameters = _service.ExtractParametersFromQuery(query);

        parameters.Should().Equal("@spid", "@status");
    }

    [Fact]
    public void ExtractParametersFromQuery_NullQuery_ThrowsArgumentNullException()
    {
        Action action = () => _service.ExtractParametersFromQuery(null!);

        action.Should().Throw<ArgumentNullException>();
    }
}