using System.Collections.Generic;

namespace SqlQueryStress.DTOs;

public class SettingsDto
{
    public bool CollectIoStats { get; set; }
    public bool CollectTimeStats { get; set; }
    public int CommandTimeout { get; set; }
    public int ConnectionTimeout { get; set; }
    public int DelayBetweenQueries { get; set; }
    public bool EnableConnectionPooling { get; set; }
    public bool ForceDataRetrieval { get; set; }
    public bool KillQueriesOnCancel { get; set; }
    public required DbConnectionInfoDto MainDbConnectionInfo { get; set; }
    public string MainQuery { get; set; } = "";
    public int NumIterations { get; set; } = 1;
    public int NumThreads { get; set; } = 1;
    public required DbConnectionInfoDto ParamDbConnectionInfo { get; set; }
    public required List<ParamMappingItemDto> ParamMappings { get; set; }
    public string ParamQuery { get; set; } = "";
    public required List<ParamMappingItemDto> ParamTypeMappings { get; set; }
    public bool ShareDbSettings { get; set; }
}