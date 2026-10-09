using FluentAssertions;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Services.LoadEngine;
using LoadEngineService = SqlQueryStress.Services.LoadEngine.LoadEngine;

namespace SqlQueryStress.Tests.Services.LoadEngine;

public class LoadEngineTests
{
    [Fact]
    public void Start_ShouldRunAllWorkers()
    {
        var outputs = new List<QueryOutput>();
        var outputLock = new object();

        using var engine = new LoadEngineService();

        for (var i = 0; i < 2; i++)
        {
            var connection = new SqlConnection();
            var command = new SqlCommand();

            var worker = new QueryWorker(
                connection,
                command,
                iterations: 2,
                output =>
                {
                    lock (outputLock)
                        outputs.Add(output);
                },
                openConnection: _ => { },
                executeQuery: _ => { },
                closeConnection: _ => { },
                paramServer: null
            );

            engine.AddWorker(worker);
        }

        engine.Start();
        engine.WaitForCompletion();

        outputs.Should().HaveCount(4);
    }

    [Fact]
    public void WaitForCompletion_ShouldAwaitForAllWorkers()
    {
        using var engine = new LoadEngineService();

        var executionCompleted = false;

        var connection = new SqlConnection();
        var command = new SqlCommand();

        var worker = new QueryWorker(
            connection,
            command,
            iterations: 1,
            _ => { },
            openConnection: _ => { },
            executeQuery: _ =>
            {
                Thread.Sleep(100);
                executionCompleted = true;
            },
            closeConnection: _ => { },
            paramServer: null
        );

        engine.AddWorker(worker);

        engine.Start();
        engine.WaitForCompletion();

        executionCompleted.Should().BeTrue();
    }

    [Fact]
    public void Outputs_ShouldContainAllWorkerResults()
    {
        using var engine = new LoadEngineService();

        var connection = new SqlConnection();
        var command = new SqlCommand();

        var worker = new QueryWorker(
            connection,
            command,
            iterations: 2,
            engine.PublishOutput,
            openConnection: _ => { },
            executeQuery: _ => { },
            closeConnection: _ => { },
            paramServer: null
        );
        
        engine.AddWorker(worker);
        
        engine.Start();
        engine.WaitForCompletion();

        engine.Outputs.Should().HaveCount(2);
        engine.Outputs.Should().OnlyContain( output => output.Error == null);
    }
}