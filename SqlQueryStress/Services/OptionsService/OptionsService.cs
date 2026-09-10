namespace SqlQueryStress.Services.OptionsService;

public class OptionsService : IOptionsService
{
    public bool EnableConnectionPooling { get; set; }
    public bool CollectIOStatistics { get; set; }
    public bool CollectTimeStatistics { get; set; }
    public bool ForceClientRetrievalOfData { get; set; }
    public bool KillQueriesOnCancel { get; set; }
    private int _connectionTimeout = 15;
    private int _commandTimeout;

    public void SetConnectionTimeout(int timeout)
    {
        _connectionTimeout = timeout;
    }

    public void SetCommandTimeout(int timeout)
    {
        _commandTimeout = timeout;
    }
}