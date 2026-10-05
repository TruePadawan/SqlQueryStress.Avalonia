using System.Data;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Services.LoadEngine;

namespace SqlQueryStress.Tests.Services.LoadEngine;

public class QueryWorkerTests
{
    [Fact]
    public void Run_ExecutesConfiguredIterationsAndPublishesOutputs()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();

        var outputs = new List<QueryOutput>();
        var executionCount = 0;
        var openCount = 0;
        var closeCount = 0;

        using var worker = new QueryWorker(
            connection,
            command,
            iterations: 3,
            outputs.Add,
            _ => openCount++,
            _ =>
            {
                executionCount++;
                Thread.Sleep(5);
            },
            _ => closeCount++,
            paramServer: null
        );

        worker.Run();

        executionCount.Should().Be(3);
        openCount.Should().Be(3);
        closeCount.Should().Be(3);

        outputs.Should().HaveCount(3);

        outputs[0].Finished.Should().BeFalse();
        outputs[1].Finished.Should().BeFalse();
        outputs[2].Finished.Should().BeTrue();

        outputs.Should().OnlyContain(output => output.Error == null);
        outputs.Should().OnlyContain(output => output.Time > TimeSpan.Zero);
    }

    [Fact]
    public void Run_WhenExecutionFails_CapturesErrorAndContinues()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();

        var outputs = new List<QueryOutput>();
        var executionCount = 0;

        var expectedException = new InvalidOperationException("query failed");

        using var worker = new QueryWorker(
            connection,
            command,
            iterations: 2,
            outputs.Add,
            _ => { },
            _ =>
            {
                executionCount++;

                if (executionCount == 1)
                    throw expectedException;
            },
            _ => { },
            paramServer: null
        );

        worker.Run();

        executionCount.Should().Be(2);

        outputs.Should().HaveCount(2);

        outputs[0].Error.Should().BeSameAs(expectedException);
        outputs[0].Finished.Should().BeFalse();

        outputs[1].Error.Should().BeNull();
        outputs[1].Finished.Should().BeTrue();
    }

    [Fact]
    public void Run_WithParamServer_PopulateNextRowBeforeExecution()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();

        var parameterTemplates = new[]
        {
            new SqlParameter("@id", SqlDbType.Int)
        };

        var rows = new object?[][]
        {
            new object?[] { 101 },
            new object?[] { 202 }
        };

        var paramServer = new ParamServer(parameterTemplates, rows);

        var outputs = new List<QueryOutput>();
        var observedValues = new List<int>();

        using var worker = new QueryWorker(
            connection,
            command,
            iterations: 2,
            outputs.Add,
            _ => { },
            queryCommand => { observedValues.Add((int)queryCommand.Parameters["@id"].Value); },
            _ => { },
            paramServer
        );

        command.Parameters.Count.Should().Be(1);

        worker.Run();

        observedValues.Should().Equal(101, 202);

        outputs.Should().HaveCount(2);
        outputs.Should().OnlyContain(output => output.Error == null);

        outputs[0].Finished.Should().BeFalse();
        outputs[1].Finished.Should().BeTrue();
    }

    [Fact]
    public void Run_WithQueryDelay_WaitsBetweenIterations()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();

        var outputs = new List<QueryOutput>();
        var executionTimes = new List<long>();

        var stopwatch = Stopwatch.StartNew();

        using var worker = new QueryWorker(
            connection,
            command,
            iterations: 2,
            outputs.Add,
            _ => { },
            _ => executionTimes.Add(stopwatch.ElapsedMilliseconds),
            _ => { },
            paramServer: null,
            queryDelay: 30
        );
    }

    [Fact]
    public void Run_WhenCancelled_StopsFurtherIterationsAndCancelsActiveCommand()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();
        using var cancellationSource = new CancellationTokenSource();

        var outputs = new List<QueryOutput>();

        var executionCount = 0;
        var cancelCount = 0;

        using var worker = new QueryWorker(
            connection,
            command,
            iterations: 3,
            outputs.Add,
            _ => { },
            _ =>
            {
                executionCount++;
                cancellationSource.Cancel();
            },
            _ => { },
            paramServer: null,
            queryDelay: 0,
            cancellationToken: cancellationSource.Token,
            cancelQuery: _ => cancelCount++
        );

        worker.Run();

        executionCount.Should().Be(1);
        cancelCount.Should().Be(1);

        outputs.Should().HaveCount(1);
        outputs[0].Error.Should().BeNull();
        outputs[0].Finished.Should().BeFalse();
    }

    [Fact]
    public void Run_WhenAlreadyCancelled_DoesNotExecute()
    {
        var connection = new SqlConnection();
        var command = new SqlCommand();
        using var cancellationSource = new CancellationTokenSource();

        cancellationSource.Cancel();

        var outputs = new List<QueryOutput>();
        var executionCount = 0;

        using var worker = new QueryWorker(
            connection,
            command,
            iterations: 3,
            outputs.Add,
            _ => { },
            _ => executionCount++,
            _ => { },
            paramServer: null,
            queryDelay: 0,
            cancellationToken: cancellationSource.Token
        );
        
        worker.Run();

        executionCount.Should().Be(0);
        outputs.Should().BeEmpty();
    }
}