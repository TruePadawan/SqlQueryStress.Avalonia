using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using SqlQueryStress.Services.DbConnectionService;
using SqlQueryStress.Services.OptionsService;
using SqlQueryStress.Services.ParameterSubstitutionService;
using SqlQueryStress.Services.QuerySettingsService;

namespace SqlQueryStress.ViewModels;

public partial class ParameterSubstitutionViewModel : ViewModelBase
{
    private readonly IParameterSubstitutionService _parameterService;
    private readonly IDbConnectionService _dbConnectionService;
    private readonly IOptionsService _optionsService;
    private readonly IQuerySettingsService _querySettingsService;

    [ObservableProperty] public partial TextDocument ParameterQueryDocument { get; set; }

    public ObservableCollection<ParameterSelection> ParameterSelections { get; }

    public ParameterSubstitutionViewModel(IParameterSubstitutionService parameterService,
        IDbConnectionService dbConnectionService, IOptionsService optionsService,
        IQuerySettingsService querySettingsService)
    {
        _parameterService = parameterService;
        _dbConnectionService = dbConnectionService;
        _optionsService = optionsService;
        _querySettingsService = querySettingsService;
        ParameterSelections = [];
        ParameterQueryDocument = new TextDocument(parameterService.ParameterQuery);
    }

    public async Task RefreshParameterSelectionAsync()
    {
        var parameters = _parameterService.ExtractParametersFromQuery(_querySettingsService.MainQuery);

        // There's nothing to map when the main query contains no SQL parameters
        if (parameters.Length == 0)
        {
            ParameterSelections.Clear();
            return;
        }

        var parameterQuery = ParameterQueryDocument.Text;
        if (string.IsNullOrWhiteSpace(parameterQuery))
        {
            ParameterSelections.Clear();
            return;
        }

        var connectionInfo = _dbConnectionService.ShareDbSettings
            ? _dbConnectionService.MainDbConnectionInfo
            : _dbConnectionService.ParamDbConnectionInfo;

        var columns = await _parameterService.GetParameterColumnsAsync
        (
            parameterQuery,
            connectionInfo,
             
            _optionsService.ConnectionTimeout
        );

        var columnNames = new List<string>{
            // keep an empty option so an existing parameter mapping can be cleared
            ""
        };

        var columnTypes = new Dictionary<string, string>();

        foreach (var column in columns)
        {
            columnNames.Add(column.Name);
            columnTypes[column.Name] = column.DataType;
        }

        // only replace the current mappings after the query schema succeeds
        // If SQL execution fails, the user does not lose the mappings already shown
        ParameterSelections.Clear();

        foreach (var parameter in parameters)
        {
            var selection = new ParameterSelection
            {
                Parameter = parameter,
                Columns = columnNames,
                ColumnTypes = columnTypes
            };

            if (
                _parameterService.ParameterMappings
                    .TryGetValue(parameter, out var existingMapping) &&
                columnTypes.ContainsKey(existingMapping.ColumnName)
            )
            {
                // restore the saved column only if it still exists in the current parameter-query result set
                selection.SelectedColumn = existingMapping.ColumnName;

                // SelectedColumn normally restores the detected SQL type
                // Preserve the saved datatype in case the user had manually overridden it
                if (!string.IsNullOrEmpty(existingMapping.DataType))
                    selection.Datatype = existingMapping.DataType;
            }
            
            ParameterSelections.Add(selection);
        }
    }

    public void SaveParameters()
    {
        _parameterService.ParameterQuery = ParameterQueryDocument.Text;

        var mappings = new Dictionary<string, ParameterMappingEntry>();

        foreach (var item in ParameterSelections)
        {
            // a parameter only has a valid mapping when the user actually selected a 
            // result column. Checking parameter here would save empty mappings
            // because Parameter itself is always populated
            if (!string.IsNullOrEmpty(item.SelectedColumn))
            {
                mappings[item.Parameter] = new ParameterMappingEntry(item.Datatype, item.SelectedColumn);
            }
        }

        _parameterService.ParameterMappings = mappings;
    }
}

public partial class ParameterSelection : ObservableObject
{
    public required string Parameter { get; set; }
    public List<string> Columns { get; set; } = [];
    public IReadOnlyDictionary<string, string> ColumnTypes { get; set; } = new Dictionary<string, string>();

    [ObservableProperty] public partial string Datatype { get; set; } = "";
    [ObservableProperty] public partial string SelectedColumn { get; set; } = "";

    partial void OnSelectedColumnChanged(string value)
    {
        // Match the original ParamWindow behaviour:
        // changing the mapped column immediately replaces the datatype with that column's detected SQL type.
        // Selecting the blank entry clears the type.
        Datatype = ColumnTypes.TryGetValue(value, out var dataType)
            ? dataType
            : "";
    }
}