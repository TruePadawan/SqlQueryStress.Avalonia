using System;

namespace SqlQueryStress.Services.LoadEngine;

internal sealed class QueryOutput
{
    public Exception? Error { get; set; }
    
    // Client-side execution
    public TimeSpan Time { get; set; }
    
    public bool Finished { get; set; }
    
    // populated later when STATISTICS IO/TIME support is ported
    public int LogicalReads { get; set; }
    
    public int CpuTime { get; set; }
    
    public int ElapsedTime { get; set; }
    
    // Populated later by the outer LoadingEngine Orchestration
    public int ActiveThreads { get; set; }
}