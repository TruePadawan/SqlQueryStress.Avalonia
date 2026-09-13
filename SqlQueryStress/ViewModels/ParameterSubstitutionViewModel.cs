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
        // TODO: Save the parameter mappings. I need to figure out how to get the selected columns from the UI
    }
}

public record ParameterSelection(string Parameter, string Datatype, List<string> Columns);