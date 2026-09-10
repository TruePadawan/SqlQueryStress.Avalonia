using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlQueryStress.ViewModels;

public partial class ParameterSubstitutionViewModel : ViewModelBase
{
    [ObservableProperty] public partial string ParameterQuery { get; set; }

    public ObservableCollection<ParameterMapping> ParameterMappings { get; }

    public ParameterSubstitutionViewModel(string parameterQuery,
        ObservableCollection<ParameterMapping> parameterMappings)
    {
        ParameterQuery = parameterQuery;
        ParameterMappings = parameterMappings;
    }
}

public record ParameterMapping(string Parameter, string Datatype, List<string> Columns);