using Microsoft.Extensions.DependencyInjection;
using SqlQueryStress.Services.OptionsService;
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
        }

        public void RegisterViewModels()
        {
            collection.AddTransient<MainViewModel>();
            collection.AddTransient<OptionsViewModel>();
        }

        public void RegisterWindows()
        {
            collection.AddTransient<OptionsWindow>();
        }
    }
}