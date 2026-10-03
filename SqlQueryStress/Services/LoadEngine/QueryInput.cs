using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace SqlQueryStress.Services.LoadEngine;

internal sealed class QueryInput : IDisposable
{
    private readonly SqlConnection _connection;
    private readonly SqlCommand _queryCommand;
    private readonly int _iterations;
    private readonly Action<QueryOutput> _publishOutput;
    private readonly ParamServer? _paramServer;
    private readonly int _queryDelay;

    private readonly Action<SqlConnection> _openConnection;
    private readonly Action<SqlCommand> _executeQuery;
    private readonly Action<SqlConnection> _closeConnection;

    internal QueryInput(
        SqlConnection connection,
        SqlCommand queryCommand,
        int iterations,
        Action<QueryOutput> publishOutput,
        ParamServer? paramServer = null,
        int queryDelay = 0)
        : this(
            connection,
            queryCommand,
            iterations,
            publishOutput,
            static connection => connection.Open(),
            static command => command.ExecuteNonQuery(),
            static connection => connection.Close(),
            paramServer,
            queryDelay)
    {
    }

    internal QueryInput(
        SqlConnection connection,
        SqlCommand queryCommand,
        int iterations,
        Action<QueryOutput> publishOutput,
        Action<SqlConnection> openConnection,
        Action<SqlCommand> executeQuery,
        Action<SqlConnection> closeConnection,
        ParamServer? paramServer,
        int queryDelay = 0)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(queryCommand);
        ArgumentNullException.ThrowIfNull(publishOutput);
        ArgumentNullException.ThrowIfNull(openConnection);
        ArgumentNullException.ThrowIfNull(executeQuery);
        ArgumentNullException.ThrowIfNull(closeConnection);

        if (iterations < 0)
            throw new ArgumentOutOfRangeException(nameof(iterations));

        if (queryDelay < 0)
            throw new ArgumentOutOfRangeException(nameof(queryDelay));

        _connection = connection;
        _queryCommand = queryCommand;
        _iterations = iterations;
        _publishOutput = publishOutput;
        _paramServer = paramServer;
        _queryDelay = queryDelay;
        
        _openConnection = openConnection;
        _executeQuery = executeQuery;
        _closeConnection = closeConnection;

        _queryCommand.Connection = _connection;

        if (_paramServer is not null)
        {
            if (_queryCommand.Parameters.Count > 0)
            {
                throw new ArgumentException(
                    "The query command must not already contain parameters " +
                    "when a parameter server is supplied.",
                    nameof(queryCommand));
            }

            _queryCommand.Parameters.AddRange(
                _paramServer.CreateParameters());
        }
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

                if (_paramServer is not null)
                {
                    _paramServer.PopulateNextRow(
                        _queryCommand.Parameters);
                }

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
            
            if(_queryDelay > 0 && i < _iterations - 1)
                Thread.Sleep(_queryDelay);
        }
    }

    public void Dispose()
    {
        _queryCommand.Dispose();
        _connection.Dispose();
    }
}