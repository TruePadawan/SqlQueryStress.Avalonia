using System.Data;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Services.LoadEngine;

namespace SqlQueryStress.UnitTests.ServiceTests;

public class ParamServerTests
{
    [Fact]
    public void CreateParameters_ReturnsClonedParameters()
    {
        var templates = new[]
        {
            new SqlParameter("@id", SqlDbType.Int),
            new SqlParameter("@name", SqlDbType.NVarChar)
            {
                Size = 50
            }
        };

        var rows = new[]
        {
            new object?[] { 1, "Alice" }
        };

        var server = new ParamServer(templates, rows);
        var parameters = server.CreateParameters();

        parameters.Length.Should().Be(2);
        
        parameters[0].ParameterName.Should().Be("@id");
        parameters[0].SqlDbType.Should().Be(SqlDbType.Int);
        
        parameters[1].ParameterName.Should().Be("@name");
        parameters[1].SqlDbType.Should().Be(SqlDbType.NVarChar);
        parameters[1].Size.Should().Be(50);
        
        // Each worker should get its own SqlParameter instances
        parameters[0].Should().NotBeSameAs(templates[0]);
        parameters[1].Should().NotBeSameAs(templates[1]);
    }

    [Fact]
    public void PopulateNextRow_StartsAtFirstRow()
    {
        var templates = new[]
        {
            new SqlParameter("@id", SqlDbType.Int),
            new SqlParameter("@name", SqlDbType.NVarChar)
        };

        var rows = new[]
        {
            new object?[] { 1, "Alice" },
            new object?[] { 2, "Bob" }
        };

        var server = new ParamServer(templates, rows);

        using var command = new SqlCommand();
        command.Parameters.AddRange(server.CreateParameters());

        server.PopulateNextRow(command.Parameters);

        command.Parameters["@id"].Value.Should().Be(1);
        command.Parameters["@name"].Value.Should().Be("Alice");
    }

    [Fact]
    public void PopulateNextRow_RotatesThroughRows()
    {
        var templates = new[]
        {
            new SqlParameter("@id", SqlDbType.Int),
        };

        var rows = new[]
        {
            new object?[] { 10 },
            new object?[] { 20 },
            new object?[] { 30 }
        };

        var server = new ParamServer(templates, rows);

        using var command = new SqlCommand();
        command.Parameters.AddRange(server.CreateParameters());

        server.PopulateNextRow(command.Parameters);
        command.Parameters["@id"].Value.Should().Be(10);
        
        server.PopulateNextRow(command.Parameters);
        command.Parameters["@id"].Value.Should().Be(20);

        server.PopulateNextRow(command.Parameters);
        command.Parameters["@id"].Value.Should().Be(30);

        // once all rows have been used, start again from row 0
        server.PopulateNextRow(command.Parameters);
        command.Parameters["@id"].Value.Should().Be(10);
    }

    [Fact]
    public void PopulateNextRow_PreservesDatabaseNull()
    {
        var templates = new[]
        {
            new SqlParameter("@name", SqlDbType.NVarChar)
        };

        var rows = new[]
        {
            new object?[] { DBNull.Value }
        };

        var server = new ParamServer(templates, rows);

        using var command = new SqlCommand();
        command.Parameters.AddRange(server.CreateParameters());

        server.PopulateNextRow(command.Parameters);

        command.Parameters["@name"].Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void PopulateNextRow_WithIncorrectParameterCount_Throws()
    {
        var templates = new[]
        {
            new SqlParameter("@id", SqlDbType.Int),
            new SqlParameter("@name", SqlDbType.NVarChar)
        };

        var rows = new[]
        {
            new object?[] { 1, "Alice" }
        };

        var server = new ParamServer(templates, rows);

        using var command = new SqlCommand();
        command.Parameters.Add(
            new SqlParameter("@id", SqlDbType.Int)
        );

        Action action = () => server.PopulateNextRow(command.Parameters);

        action
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("The parameter collection does not match the configured mappings");
    }
}