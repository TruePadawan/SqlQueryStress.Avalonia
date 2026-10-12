using System;

namespace SqlQueryStress.Services.LoadEngine;

/// <summary>
/// Represents the output of a query execution
/// </summary>
internal sealed class QueryOutput
{
    public Exception? Error { get; set; }
    
    // Client-side execution
    public TimeSpan Time { get; set; }
    
    public bool Finished { get; set; }
    
    public int LogicalReads { get; set; }

    public bool HasIoStatistics { get; set; }
    
    public int CpuTime { get; set; }
    
    public int ElapsedTime { get; set; }

    public bool HasTimeStatistics { get; set; }
    
    // Populated later by the outer LoadingEngine Orchestration
    public int ActiveThreads { get; set; }
}
