using FluentAssertions;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Services.LoadEngine;

namespace SqlQueryStress.Tests.Services.LoadEngine;

public class QueryInputTests
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

        using var worker = new QueryInput(
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
            _ => closeCount++
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

        using var worker = new QueryInput(
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
            _ => { }
        );
        
        worker.Run();
        
        executionCount.Should().Be(2);
        
        outputs.Should().HaveCount(2);
        
        outputs[0].Error.Should().BeSameAs(expectedException);
        outputs[0].Finished.Should().BeFalse();

        outputs[1].Error.Should().BeNull();
        outputs[1].Finished.Should().BeTrue();
    }
}