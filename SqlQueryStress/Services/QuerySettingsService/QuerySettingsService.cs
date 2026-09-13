namespace SqlQueryStress.Services.QuerySettingsService;

public class QuerySettingsService : IQuerySettingsService
{
    public string MainQuery { get; set; } = "";
    public int NumIterations { get; set; } = 1;
    public int NumThreads { get; set; } = 1;
    public int DelayBetweenQueries { get; set; } = 0;
}