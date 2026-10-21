using FluentAssertions;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Services.LoadEngine;

namespace SqlQueryStress.UnitTests.ServiceTests;

public class LoadEngineAdditionalTests
{
    [Fact]
    public void ActiveThreadCount_StartsAtWorkerCount()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();
        var started = new ManualResetEventSlim(false);

        using var engine = new LoadEngine();
        engine.AddWorker(new QueryWorker(
            connection,
            command,
            iterations: 1,
            publishOutput: _ => { },
            openConnection: _ => { },
            executeQuery: _ =>
            {
                started.Set();
                Thread.Sleep(200);
            },
            closeConnection: _ => { },
            paramServer: null
        ));

        engine.Start();
        started.Wait();

        engine.ActiveThreadCount.Should().Be(1);
        engine.WaitForCompletion();
    }

    [Fact]
    public void ActiveThreadCount_DecrementsAsThreadsFinish()
    {
        var engine = new LoadEngine();
        var started = new ManualResetEventSlim(false);

        for (var i = 0; i < 3; i++)
        {
            var connection = new SqlConnection();
            var command = new SqlCommand();
            engine.AddWorker(new QueryWorker(
                connection,
                command,
                iterations: 1,
                publishOutput: _ => { },
                openConnection: _ => { },
                executeQuery: _ =>
                {
                    started.Set();
                    Thread.Sleep(200);
                },
                closeConnection: _ => { },
                paramServer: null
            ));
        }

        engine.Start();
        started.Wait();

        engine.ActiveThreadCount.Should().Be(3);
        engine.WaitForCompletion();
        engine.ActiveThreadCount.Should().Be(0);

        engine.Dispose();
    }

    [Fact]
    public void AddWorker_Null_ThrowsArgumentNullException()
    {
        using var engine = new LoadEngine();

        var action = () => engine.AddWorker(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PublishOutput_Null_ThrowsArgumentNullException()
    {
        using var engine = new LoadEngine();

        Action action = () => engine.PublishOutput(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FactoryAddWorker_CreatesWorkerWithPublishOutput()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();

        using var engine = new LoadEngine();
        engine.AddWorker(
            connection,
            command,
            iterations: 1,
            paramServer: null,
            queryDelay: 0,
            cancellationToken: CancellationToken.None
        );

        engine.Start();
        engine.WaitForCompletion();

        engine.Outputs.Should().HaveCount(1);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();

        var engine = new LoadEngine();
        engine.AddWorker(new QueryWorker(
            connection,
            command,
            iterations: 0,
            publishOutput: _ => { },
            openConnection: _ => { },
            executeQuery: _ => { },
            closeConnection: _ => { },
            paramServer: null
        ));

        // Dispose should complete without throwing
        var action = engine.Dispose;
        action.Should().NotThrow();
    }
}