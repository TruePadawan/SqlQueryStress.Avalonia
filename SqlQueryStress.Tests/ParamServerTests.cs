using System;
using System.Data;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Services.LoadEngine;
using Xunit;

namespace SqlQueryStress.Tests;

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

        Assert.Equal(2, parameters.Length);

        Assert.Equal("@id", parameters[0].ParameterName);
        Assert.Equal(SqlDbType.Int, parameters[0].SqlDbType);

        Assert.Equal("@name", parameters[1].ParameterName);
        Assert.Equal(SqlDbType.NVarChar, parameters[1].SqlDbType);
        Assert.Equal(50, parameters[1].Size);

        // Each worker should get its own SqlParameter instances
        Assert.NotSame(templates[0], parameters[0]);
        Assert.NotSame(templates[1], parameters[1]);
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

        Assert.Equal(1, command.Parameters["@id"].Value);
        Assert.Equal("Alice", command.Parameters["@name"].Value);
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
        Assert.Equal(10, command.Parameters["@id"].Value);

        server.PopulateNextRow(command.Parameters);
        Assert.Equal(20, command.Parameters["@id"].Value);

        server.PopulateNextRow(command.Parameters);
        Assert.Equal(30, command.Parameters["@id"].Value);

        // once all rows have been used, start again from row 0
        server.PopulateNextRow(command.Parameters);
        Assert.Equal(10, command.Parameters["@id"].Value);
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

        Assert.Equal(
            DBNull.Value,
            command.Parameters["@name"].Value
        );
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
        
        Assert.Throws<InvalidOperationException>(
            () => server.PopulateNextRow(command.Parameters));
    }
}