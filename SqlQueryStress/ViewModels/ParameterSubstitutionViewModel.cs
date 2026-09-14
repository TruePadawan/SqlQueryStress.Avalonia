using System.Collections.Generic;
using System.Collections.ObjectModel;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using SqlQueryStress.Services.ParameterSubstitutionService;

namespace SqlQueryStress.ViewModels;

public partial class ParameterSubstitutionViewModel : ViewModelBase
{
    private readonly IParameterSubstitutionService _parameterService;

    [ObservableProperty] public partial TextDocument ParameterQueryDocument { get; set; }
    public ObservableCollection<ParameterSelection> ParameterSelections { get; }

    public ParameterSubstitutionViewModel(IParameterSubstitutionService parameterService)
    {
        _parameterService = parameterService;
        ParameterSelections = [];
        ParameterQueryDocument = new TextDocument(parameterService.ParameterQuery);

        // TODO: If parameter query is not empty, run the SQL Query to get the columns
        //  and match the parameter names to their actual columns in ParameterMappings
    }

    public void SaveParameters()
    {
        _parameterService.ParameterQuery = ParameterQueryDocument.Text;

        var mappings = new Dictionary<string, ParameterMappingEntry>();
        foreach (var item in ParameterSelections)
        {
            if (!string.IsNullOrEmpty(item.Parameter))
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
    public string Datatype { get; set; } = "";
    public List<string> Columns { get; set; } = [];

    [ObservableProperty] public partial string SelectedColumn { get; set; } = "";
}