using System;

namespace SqlQueryStress.Services.BenchmarkExportService;

/// <summary>
/// Snapshot of a completed load test, used to render benchmark exports.
/// Nullable averages mirror the original tool, which shows "---" when no
/// statistic messages were collected.
/// </summary>
public sealed class BenchmarkResult
{
    public required Guid TestId { get; init; }
    public required DateTime TestStartTime { get; init; }
    public required TimeSpan Elapsed { get; init; }
    public required int NumIterations { get; init; }
    public required int NumThreads { get; init; }
    public required int DelayBetweenQueries { get; init; }
    public required int CompletedIterations { get; init; }
    public required double AverageClientSeconds { get; init; }
    public double? AverageCpuSeconds { get; init; }
    public double? AverageActualSeconds { get; init; }
    public double? AverageLogicalReads { get; init; }
}