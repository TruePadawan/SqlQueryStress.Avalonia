namespace SqlQueryStress.Services.OptionsService;

public class OptionsService : IOptionsService
{
    public bool EnableConnectionPooling { get; set; } = true;
    public bool CollectIOStatistics { get; set; } = true;
    public bool CollectTimeStatistics { get; set; } = true;
    public bool ForceClientRetrievalOfData { get; set; }
    public bool KillQueriesOnCancel { get; set; } = true;
    public int ConnectionTimeout { get; set; } = 15;
    public int CommandTimeout { get; set; } = 0;
}