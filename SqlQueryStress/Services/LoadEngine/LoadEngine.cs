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

    /// <summary>
    /// Number of worker threads that are still running. The backing field is set to the
    /// worker count in <see cref="Start"/> and decremented as each thread finishes.
    /// </summary>
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

    /// <summary>
    /// Starts a dedicated thread for each worker and returns immediately.
    /// </summary>
    internal void Start()
    {
        // Seed the active-thread count with the number of workers about to start
        Volatile.Write(ref _activeThreadCount, _workers.Count);

        for (var i = 0; i < _workers.Count; i++)
        {
            var worker = _workers[i];

            var thread = new Thread(() =>
                {
                    try
                    {
                        worker.Run();
                    }
                    finally
                    {
                        // This thread is done, so it no longer counts as active
                        Interlocked.Decrement(ref _activeThreadCount);
                    }
                })
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = $"thread: {i}"
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
