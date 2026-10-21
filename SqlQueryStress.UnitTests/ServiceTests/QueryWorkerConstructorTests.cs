using FluentAssertions;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Services.LoadEngine;

namespace SqlQueryStress.UnitTests.ServiceTests;

public class QueryWorkerConstructorTests
{
    [Fact]
    public void Constructor_NegativeIterations_ThrowsArgumentOutOfRangeException()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();

        Action action = () => new QueryWorker(
            connection,
            command,
            iterations: -1,
            publishOutput: _ => { },
            openConnection: _ => { },
            executeQuery: _ => { },
            closeConnection: _ => { },
            paramServer: null
        );

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeQueryDelay_ThrowsArgumentOutOfRangeException()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();

        Action action = () => new QueryWorker(
            connection,
            command,
            iterations: 1,
            publishOutput: _ => { },
            openConnection: _ => { },
            executeQuery: _ => { },
            closeConnection: _ => { },
            paramServer: null,
            queryDelay: -1
        );

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        var command = new SqlCommand();

        Action action = () => new QueryWorker(
            null!,
            command,
            iterations: 1,
            publishOutput: _ => { },
            openConnection: _ => { },
            executeQuery: _ => { },
            closeConnection: _ => { },
            paramServer: null
        );

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullCommand_ThrowsArgumentNullException()
    {
        var connection = new SqlConnection();

        Action action = () => new QueryWorker(
            connection,
            null!,
            iterations: 1,
            publishOutput: _ => { },
            openConnection: _ => { },
            executeQuery: _ => { },
            closeConnection: _ => { },
            paramServer: null
        );

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullPublishOutput_ThrowsArgumentNullException()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();

        Action action = () => new QueryWorker(
            connection,
            command,
            iterations: 1,
            publishOutput: null!,
            openConnection: _ => { },
            executeQuery: _ => { },
            closeConnection: _ => { },
            paramServer: null
        );

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ParamServerWithPreExistingParameters_ThrowsArgumentException()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();
        command.Parameters.Add(new SqlParameter("@existing", 1));

        var parameterTemplates = new[]
        {
            new SqlParameter("@id", System.Data.SqlDbType.Int)
        };
        var rows = new object?[][] { [1] };
        var paramServer = new ParamServer(parameterTemplates, rows);

        Action action = () => new QueryWorker(
            connection,
            command,
            iterations: 1,
            publishOutput: _ => { },
            openConnection: _ => { },
            executeQuery: _ => { },
            closeConnection: _ => { },
            paramServer: paramServer
        );

        action.Should().Throw<ArgumentException>();
    }
}