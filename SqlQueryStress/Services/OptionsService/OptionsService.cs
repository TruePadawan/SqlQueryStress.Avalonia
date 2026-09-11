namespace SqlQueryStress.Services.OptionsService;

public class OptionsService : IOptionsService
{
    public bool EnableConnectionPooling { get; set; }
    public bool CollectIOStatistics { get; set; }
    public bool CollectTimeStatistics { get; set; }
    public bool ForceClientRetrievalOfData { get; set; }
    public bool KillQueriesOnCancel { get; set; }
    public int ConnectionTimeout { get; set; } = 15;
    public int CommandTimeout { get; set; } = 0;
}