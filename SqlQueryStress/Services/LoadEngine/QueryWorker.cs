using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace SqlQueryStress.Services.LoadEngine;

/// <summary>
/// Represents a worker that executes an SQL query
/// </summary>
internal sealed class QueryWorker : IDisposable
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

    private static readonly Regex FindReads = new(
        @"logical reads\s+(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex FindTimes = new(
        @"(?:SQL Server Execution Times:|SQL Server parse and compile time:)\s*" +
        @"CPU time =\s*(\d+)\s*ms,\s*elapsed time =\s*(\d+)\s*ms",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly bool _collectIoStats;
    private readonly bool _collectTimeStats;
    private readonly bool _forceDataRetrieval;

    private QueryOutput? _currentOutput;

    internal QueryWorker(
        SqlConnection connection,
        SqlCommand queryCommand,
        int iterations,
        Action<QueryOutput> publishOutput,
        ParamServer? paramServer = null,
        int queryDelay = 0,
        CancellationToken cancellationToken = default,
        bool collectIoStats = false,
        bool collectTimeStats = false,
        bool forceDataRetrieval = false
    )
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
            collectIoStats,
            collectTimeStats,
            forceDataRetrieval
        )
    {
    }

    internal QueryWorker(
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
        bool collectIoStats = false,
        bool collectTimeStats = false,
        bool forceDataRetrieval = false
    )
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

        _openConnection = openConnection;
        _executeQuery = executeQuery;
        _closeConnection = closeConnection;

        _queryCommand.Connection = _connection;

        _collectIoStats = collectIoStats;
        _collectTimeStats = collectTimeStats;
        _forceDataRetrieval = forceDataRetrieval;

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

    private void GetInfoMessages(object sender, SqlInfoMessageEventArgs args)
    {
        var output = _currentOutput;

        if (output is null)
            return;

        foreach (SqlError error in args.Errors)
            ApplyStatisticsMessage(output, error.Message);
    }

    internal static void ApplyStatisticsMessage(QueryOutput output, string message)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(message);

        foreach (Match readsMatch in FindReads.Matches(message))
        {
            if (readsMatch.Success &&
                int.TryParse(
                    readsMatch.Groups[1].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var reads))
            {
                output.HasIoStatistics = true;
                output.LogicalReads += reads;
            }
        }

        foreach (Match timesMatch in FindTimes.Matches(message))
        {
            if (timesMatch.Success &&
                int.TryParse(
                    timesMatch.Groups[1].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var cpuTime) &&
                int.TryParse(
                    timesMatch.Groups[2].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var elapsedTime))
            {
                output.HasTimeStatistics = true;
                output.CpuTime += cpuTime;
                output.ElapsedTime += elapsedTime;
            }
        }
    }

    private void ExecuteQuery()
    {
        if (!_forceDataRetrieval)
        {
            _executeQuery(_queryCommand);
            return;
        }

        using var reader = _queryCommand.ExecuteReader();

        do
        {
            while (reader.Read())
            {
                // access a column to force the result data to be retrieved
                if (reader.FieldCount > 0)
                    _ = reader.GetValue(0);
            }
        } while (reader.NextResult());
    }

    internal void Run()
    {
        for (var i = 0; i < _iterations; i++)
        {
            if (_cancellationToken.IsCancellationRequested)
                break;

            var output = new QueryOutput();
            var stopwatch = new Stopwatch();
            var collectStatistics = _collectIoStats || _collectTimeStats;

            try
            {
                _openConnection(_connection);

                if (_paramServer is not null)
                    _paramServer.PopulateNextRow(_queryCommand.Parameters);

                if (_cancellationToken.IsCancellationRequested)
                    break;

                _currentOutput = output;

                if (collectStatistics)
                {
                    var statistics = string.Concat(
                        _collectIoStats
                            ? "SET STATISTICS IO ON;"
                            : string.Empty,
                        _collectTimeStats
                            ? "SET STATISTICS TIME ON;"
                            : string.Empty
                    );

                    using var statisticsCommand = new SqlCommand(
                        statistics,
                        _connection)
                    {
                        CommandTimeout = _queryCommand.CommandTimeout
                    };

                    statisticsCommand.ExecuteNonQuery();
                    _connection.InfoMessage += GetInfoMessages;
                }

                try
                {
                    // Exclude connection setup and parameter rotation from client-side time
                    stopwatch.Start();
                    ExecuteQuery();
                }
                finally
                {
                    stopwatch.Stop();

                    if (collectStatistics)
                        _connection.InfoMessage -= GetInfoMessages;

                    DisableStatistics(collectStatistics);
                }
            }
            catch (Exception exception)
            {
                output.Error = exception;
            }
            finally
            {
                output.Time = stopwatch.Elapsed;
                output.Finished = i == _iterations - 1;

                _currentOutput = null;
                _closeConnection(_connection);
            }

            _publishOutput(output);

            // cancellation might have occurred while opening the connection
            // or preparing the next parameter row
            if (_cancellationToken.IsCancellationRequested)
                break;

            if (_queryDelay > 0 && i < _iterations - 1)
                Thread.Sleep(_queryDelay);
        }
    }

    private void DisableStatistics(bool collectStatistics)
    {
        if (!collectStatistics || _connection.State != ConnectionState.Open)
            return;

        var statistics = string.Concat(
            _collectIoStats
                ? "SET STATISTICS IO OFF;"
                : string.Empty,
            _collectTimeStats
                ? "SET STATISTICS TIME OFF;"
                : string.Empty
        );

        using var statisticsCommand = new SqlCommand(statistics, _connection);
        statisticsCommand.CommandTimeout = _queryCommand.CommandTimeout;

        statisticsCommand.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _queryCommand.Dispose();
        _connection.Dispose();
    }
}