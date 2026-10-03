using System;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace SqlQueryStress.Services.LoadEngine;

internal sealed class QueryInput : IDisposable
{
    private readonly SqlConnection _connection;
    private readonly SqlCommand _queryCommand;
    private readonly int _iterations;
    private readonly Action<QueryOutput> _publishOutput;

    private readonly Action<SqlConnection> _openConnection;
    private readonly Action<SqlCommand> _executeQuery;
    private readonly Action<SqlConnection> _closeConnection;

    internal QueryInput(
        SqlConnection connection,
        SqlCommand queryCommand,
        int iterations,
        Action<QueryOutput> publishOutput)
        : this(
            connection,
            queryCommand,
            iterations,
            publishOutput,
            static connection => connection.Open(),
            static command => command.ExecuteNonQuery(),
            static connection => connection.Close())
    {
    }

    // Internal execution hooks keep the worker unit-testable without
    // requiring a live SQL Server instance.
    internal QueryInput(
        SqlConnection connection,
        SqlCommand queryCommand,
        int iterations,
        Action<QueryOutput> publishOutput,
        Action<SqlConnection> openConnection,
        Action<SqlCommand> executeQuery,
        Action<SqlConnection> closeConnection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(queryCommand);
        ArgumentNullException.ThrowIfNull(publishOutput);
        ArgumentNullException.ThrowIfNull(openConnection);
        ArgumentNullException.ThrowIfNull(executeQuery);
        ArgumentNullException.ThrowIfNull(closeConnection);

        if (iterations < 0)
            throw new ArgumentOutOfRangeException(nameof(iterations));

        _connection = connection;
        _queryCommand = queryCommand;
        _iterations = iterations;
        _publishOutput = publishOutput;

        _openConnection = openConnection;
        _executeQuery = executeQuery;
        _closeConnection = closeConnection;

        _queryCommand.Connection = _connection;
    }

    internal void Run()
    {
        for (var i = 0; i < _iterations; i++)
        {
            Exception? error = null;
            var stopwatch = new Stopwatch();

            try
            {
                _openConnection(_connection);

                stopwatch.Start();
                _executeQuery(_queryCommand);
                stopwatch.Stop();
            }
            catch (Exception ex)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();

                error = ex;
            }
            finally
            {
                _closeConnection(_connection);
            }

            _publishOutput(
                new QueryOutput
                {
                    Error = error,
                    Time = stopwatch.Elapsed,
                    Finished = i == _iterations - 1
                });
        }
    }

    public void Dispose()
    {
        _queryCommand.Dispose();
        _connection.Dispose();
    }
}