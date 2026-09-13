namespace SqlQueryStress.Services.QuerySettingsService;

public interface IQuerySettingsService
{
    string MainQuery { get; set; }
    int NumIterations { get; set; }
    int NumThreads { get; set; }
    int DelayBetweenQueries { get; set; }
}