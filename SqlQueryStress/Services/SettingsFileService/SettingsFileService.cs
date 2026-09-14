using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using SqlQueryStress.DTOs;
using SqlQueryStress.Services.DbConnectionService;
using SqlQueryStress.Services.OptionsService;
using SqlQueryStress.Services.ParameterSubstitutionService;
using SqlQueryStress.Services.QuerySettingsService;

namespace SqlQueryStress.Services.SettingsFileService;

public class SettingsFileService : ISettingsFileService
{
    private readonly IOptionsService _optionsService;
    private readonly IDbConnectionService _dbConnectionService;
    private readonly IParameterSubstitutionService _parameterSubstitutionService;
    private readonly IQuerySettingsService _querySettingsService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public SettingsFileService(
        IOptionsService optionsService,
        IDbConnectionService dbConnectionService,
        IParameterSubstitutionService parameterSubstitutionService,
        IQuerySettingsService querySettingsService)
    {
        _optionsService = optionsService;
        _dbConnectionService = dbConnectionService;
        _parameterSubstitutionService = parameterSubstitutionService;
        _querySettingsService = querySettingsService;
    }

    public async Task SaveAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        await using var stream = File.Create(filePath);
        await SaveAsync(stream);
    }

    public async Task SaveAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var dto = new SettingsDto
        {
            // IOptionsService
            EnableConnectionPooling = _optionsService.EnableConnectionPooling,
            CollectIoStats = _optionsService.CollectIOStatistics,
            CollectTimeStats = _optionsService.CollectTimeStatistics,
            ForceDataRetrieval = _optionsService.ForceClientRetrievalOfData,
            KillQueriesOnCancel = _optionsService.KillQueriesOnCancel,
            ConnectionTimeout = _optionsService.ConnectionTimeout,
            CommandTimeout = _optionsService.CommandTimeout,

            // IQuerySettingsService
            MainQuery = _querySettingsService.MainQuery,
            NumIterations = _querySettingsService.NumIterations,
            NumThreads = _querySettingsService.NumThreads,
            DelayBetweenQueries = _querySettingsService.DelayBetweenQueries,

            // IDbConnectionService
            ShareDbSettings = _dbConnectionService.ShareDbSettings,
            MainDbConnectionInfo = DbConnectionInfoDto.FromModel(_dbConnectionService.MainDbConnectionInfo,
                _optionsService.ConnectionTimeout),
            ParamDbConnectionInfo = DbConnectionInfoDto.FromModel(_dbConnectionService.ParamDbConnectionInfo,
                _optionsService.ConnectionTimeout),

            // IParameterSubstitutionService
            ParamQuery = _parameterSubstitutionService.ParameterQuery,
            ParamMappings = _parameterSubstitutionService.ParameterMappings?
                .Select(kv => new ParamMappingItemDto { Key = kv.Key, Value = kv.Value.ColumnName })
                .ToList() ?? [],
            ParamTypeMappings = _parameterSubstitutionService.ParameterMappings?
                .Select(kv => new ParamMappingItemDto { Key = kv.Key, Value = kv.Value.DataType })
                .ToList() ?? []
        };

        await JsonSerializer.SerializeAsync(stream, dto, JsonOptions);
    }

    public async Task LoadAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        await using var stream = File.OpenRead(filePath);
        await LoadAsync(stream);
    }

    public async Task LoadAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var dto = await JsonSerializer.DeserializeAsync<SettingsDto>(stream, JsonOptions);
        if (dto == null) return;

        // Populate IOptionsService
        _optionsService.EnableConnectionPooling = dto.EnableConnectionPooling;
        _optionsService.CollectIOStatistics = dto.CollectIoStats;
        _optionsService.CollectTimeStatistics = dto.CollectTimeStats;
        _optionsService.ForceClientRetrievalOfData = dto.ForceDataRetrieval;
        _optionsService.KillQueriesOnCancel = dto.KillQueriesOnCancel;
        _optionsService.ConnectionTimeout = dto.ConnectionTimeout;
        _optionsService.CommandTimeout = dto.CommandTimeout;

        // Populate IQuerySettingsService
        _querySettingsService.MainQuery = dto.MainQuery;
        _querySettingsService.NumIterations = dto.NumIterations > 0 ? dto.NumIterations : 1;
        _querySettingsService.NumThreads = dto.NumThreads > 0 ? dto.NumThreads : 1;
        _querySettingsService.DelayBetweenQueries = dto.DelayBetweenQueries >= 0 ? dto.DelayBetweenQueries : 0;

        // Populate IDbConnectionService
        _dbConnectionService.ShareDbSettings = dto.ShareDbSettings;
        _dbConnectionService.UpdateMainDbConnectionInfo(DbConnectionInfoDto.ToModel(dto.MainDbConnectionInfo));
        _dbConnectionService.UpdateParamDbConnectionInfo(DbConnectionInfoDto.ToModel(dto.ParamDbConnectionInfo));

        // Populate IParameterSubstitutionService
        _parameterSubstitutionService.ParameterQuery = dto.ParamQuery;

        var paramMappingsList = dto.ParamMappings;
        var paramTypeMappingsList = dto.ParamTypeMappings;
        // Get all unique parameters
        var keys = paramMappingsList.Select(m => m.Key)
            .Concat(paramTypeMappingsList.Select(m => m.Key))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var dict = new Dictionary<string, ParameterMappingEntry>();
        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(key)) continue;
            var colName = paramMappingsList
                .FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))?.Value ?? "";
            var dataType = paramTypeMappingsList
                .FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))?.Value ?? "";
            dict[key] = new ParameterMappingEntry(dataType, colName);
        }

        _parameterSubstitutionService.ParameterMappings = dict;
    }
}