using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace SqlQueryStress.Services.LoadEngine;

/// <summary>
/// coordinates query workers and their dedicated execution threads
/// </summary>
internal sealed class LoadEngine : IDisposable
{
    private readonly List<QueryWorker> _workers = [];
    private readonly List<Thread> _threads = [];
    private readonly List<QueryOutput> _outputs = [];

    private readonly object _outputLock = new();
    private int _activeThreadCount;

    internal IReadOnlyList<QueryOutput> Outputs
    {
        get
        {
            lock (_outputLock)
                return [.. _outputs];
        }
    }

    internal int ActiveThreadCount => Volatile.Read(ref _activeThreadCount);

    internal void AddWorker(QueryWorker worker)
    {
        ArgumentNullException.ThrowIfNull(worker);
        _workers.Add(worker);
    }

    internal void AddWorker(
        SqlConnection connection,
        SqlCommand command,
        int iterations,
        ParamServer? paramServer,
        int queryDelay,
        CancellationToken cancellationToken
    )
    {
        var worker = new QueryWorker(
            connection,
            command,
            iterations,
            PublishOutput,
            paramServer,
            queryDelay,
            cancellationToken
        );

        _workers.Add(worker);
    }

    internal void Start()
    {
        Volatile.Write(ref _activeThreadCount, _workers.Count);

        foreach (var worker in _workers)
        {
            var thread = new Thread(() =>
                {
                    try
                    {
                        worker.Run();
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _activeThreadCount);
                    }
                })
            {
                IsBackground = true
            };

            _threads.Add(thread);
            thread.Start();
        }
    }

    internal void WaitForCompletion()
    {
        foreach (var thread in _threads)
            thread.Join();
    }

    internal void PublishOutput(QueryOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        lock (_outputLock)
            _outputs.Add(output);
    }

    public void Dispose()
    {
        foreach (var worker in _workers)
            worker.Dispose();
    }
}
