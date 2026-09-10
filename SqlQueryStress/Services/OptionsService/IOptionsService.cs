namespace SqlQueryStress.Services.OptionsService;

public interface IOptionsService
{
    bool EnableConnectionPooling { get; set; }
    bool CollectIOStatistics { get; set; }
    bool CollectTimeStatistics { get; set; }
    bool ForceClientRetrievalOfData { get; set; }
    bool KillQueriesOnCancel { get; set; }


    void SetConnectionTimeout(int timeout);
    void SetCommandTimeout(int timeout);
}