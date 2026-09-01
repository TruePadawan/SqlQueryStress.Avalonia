using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlQueryStress.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Greeting { get; set; } = "Welcome to Avalonia!";
}