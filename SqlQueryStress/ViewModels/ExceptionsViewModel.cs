using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlQueryStress.ViewModels;

public partial class ExceptionsViewModel : ViewModelBase
{
    public ObservableCollection<ExceptionItem> Exceptions { get; }

    public ExceptionsViewModel(ObservableCollection<ExceptionItem> exceptions)
    {
        Exceptions = exceptions;
    }
}

public record ExceptionItem(int Count, string Exception);
