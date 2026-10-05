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

    private readonly CancellationToken _cancellationToken;
    private readonly Action<SqlCommand> _cancelQuery;

    internal QueryInput(
        SqlConnection connection,
        SqlCommand queryCommand,
        int iterations,
        Action<QueryOutput> publishOutput,
        ParamServer? paramServer = null,
        int queryDelay = 0,
        CancellationToken cancellationToken = default)
        : this(
            connection,
            queryCommand,
            iterations,
            publishOutput,
            static connection => connection.Open(),
            static command => command.ExecuteNonQuery(),
            static connection => connection.Close(),
            paramServer,
            queryDelay,
            cancellationToken,
            static command => command.Cancel())
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
        int queryDelay = 0,
        CancellationToken cancellationToken = default,
        Action<SqlCommand>? cancelQuery = null)
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
        _cancellationToken = cancellationToken;
        _cancelQuery = cancelQuery ?? (Action<SqlCommand>)(static command => command.Cancel());

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
        var queryExecuting = 0;
        
        // Cancellation can arrive from another thread
        // only cancel the command while query execution is in progress
        using var cancellationRegistration =
            _cancellationToken.Register(() =>
                {
                    if (Volatile.Read(ref queryExecuting) == 1)
                        _cancelQuery(_queryCommand);
                }
            );

        for (var i = 0; i < _iterations; i++)
        {
            if (_cancellationToken.IsCancellationRequested)
                break;

            Exception? error = null;
            var stopwatch = new Stopwatch();

            try
            {
                _openConnection(_connection);

                if (_paramServer is not null)
                    _paramServer.PopulateNextRow(
                        _queryCommand.Parameters);

                if (_cancellationToken.IsCancellationRequested)
                    break;

                Volatile.Write(ref queryExecuting, 1);
                
                // Measure query execution only; connection setup and parameter rotation
                // are intentionally excluded from the client-side execution time
                stopwatch.Start();
                _executeQuery(_queryCommand);
                stopwatch.Stop();
            }
            catch (Exception exception)
            {
                if (stopwatch.IsRunning)
                    stopwatch.Stop();

                if (!_cancellationToken.IsCancellationRequested)
                    error = exception;
            }
            finally
            {
                Volatile.Write(ref queryExecuting, 0);
                _closeConnection(_connection);
            }

            _publishOutput(
                new QueryOutput
                {
                    Error = error,
                    Time = stopwatch.Elapsed,
                    Finished = i == _iterations - 1
                });
            
            // cancellation might have occured while opening the connection
            // or preparing the next parameter row
            if (_cancellationToken.IsCancellationRequested)
                break;
            
            // use a cancellation-aware wait so that a worker doesn't remain blocked
            // in the inter-query delay after the load has been cancelled
            if(_queryDelay > 0 && i < _iterations - 1)
                if (_cancellationToken.WaitHandle.WaitOne(_queryDelay))
                    break;
        }
    }

    public void Dispose()
    {
        _queryCommand.Dispose();
        _connection.Dispose();
    }
}