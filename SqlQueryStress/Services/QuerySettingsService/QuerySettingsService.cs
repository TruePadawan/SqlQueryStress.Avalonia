namespace SqlQueryStress.Services.QuerySettingsService;

public class QuerySettingsService : IQuerySettingsService
{
    public string MainQuery { get; set; } = "";
    public int NumIterations { get; set; }
    public int NumThreads { get; set; }
    public int DelayBetweenQueries { get; set; }
}