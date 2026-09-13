using Microsoft.Extensions.DependencyInjection;
using SqlQueryStress.Services.DbConnectionService;
using SqlQueryStress.Services.OptionsService;
using SqlQueryStress.Services.ParameterSubstitutionService;
using SqlQueryStress.ViewModels;
using SqlQueryStress.Views;

namespace SqlQueryStress.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        public void RegisterServices()
        {
            collection.AddSingleton<IOptionsService, OptionsService>();
            collection.AddSingleton<IDbConnectionService, DbConnectionService>();
            collection.AddSingleton<IParameterSubstitutionService, ParameterSubstitutionService>();
        }

        public void RegisterViewModels()
        {
            collection.AddTransient<MainViewModel>();
            collection.AddTransient<OptionsViewModel>();
            collection.AddTransient<DatabaseSelectViewModel>();
            collection.AddTransient<ParameterSubstitutionViewModel>();
        }

        public void RegisterWindows()
        {
            collection.AddTransient<OptionsWindow>();
            collection.AddTransient<DatabaseSelectWindow>();
            collection.AddTransient<ParameterSubstitutionWindow>();
        }
    }
}