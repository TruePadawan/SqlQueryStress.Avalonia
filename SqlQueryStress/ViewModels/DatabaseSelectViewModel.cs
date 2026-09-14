using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SqlQueryStress.Models;
using SqlQueryStress.Services.DbConnectionService;

namespace SqlQueryStress.ViewModels;

public partial class DatabaseSelectViewModel : ViewModelBase
{
    private readonly IDbConnectionService _dbConnectionService;

    // Option arrays for combo boxes
    public AuthenticationModeOption[] AuthenticationModeOptions { get; }
    public ApplicationIntent[] ApplicationIntentOptions { get; } = Enum.GetValues<ApplicationIntent>();
    public EncryptOption[] EncryptOptions { get; } = Enum.GetValues<EncryptOption>();

    [ObservableProperty] public partial bool ShareDbSettings { get; set; }

    // Main section observable properties
    [ObservableProperty] public partial string MainServer { get; set; }
    [ObservableProperty] public partial AuthenticationModeOption SelectedMainDbAuthenticationMode { get; set; }
    [ObservableProperty] public partial string MainLogin { get; set; }
    [ObservableProperty] public partial string MainPassword { get; set; }
    [ObservableProperty] public partial string MainDatabase { get; set; }
    [ObservableProperty] public partial bool MainApplicationIntentEnabled { get; set; }
    [ObservableProperty] public partial ApplicationIntent MainApplicationIntent { get; set; }
    [ObservableProperty] public partial EncryptOption MainEncryptOption { get; set; }
    [ObservableProperty] public partial bool MainTrustServerCertificate { get; set; }
    [ObservableProperty] public partial string MainAdditionalParameters { get; set; }

    // Param section observable properties
    [ObservableProperty] public partial string ParamServer { get; set; }
    [ObservableProperty] public partial AuthenticationModeOption SelectedParamDbAuthenticationMode { get; set; }
    [ObservableProperty] public partial string ParamLogin { get; set; }
    [ObservableProperty] public partial string ParamPassword { get; set; }
    [ObservableProperty] public partial string ParamDatabase { get; set; }
    [ObservableProperty] public partial bool ParamApplicationIntentEnabled { get; set; }
    [ObservableProperty] public partial ApplicationIntent ParamApplicationIntent { get; set; }
    [ObservableProperty] public partial EncryptOption ParamEncryptOption { get; set; }
    [ObservableProperty] public partial bool ParamTrustServerCertificate { get; set; }
    [ObservableProperty] public partial string ParamAdditionalParameters { get; set; }

    public DatabaseSelectViewModel(IDbConnectionService dbConnectionService)
    {
        _dbConnectionService = dbConnectionService;

        // Build authentication mode options
        AuthenticationModeOptions =
        [
            .. Enum.GetValues<AuthenticationMode>().Select(AuthenticationModeOption.From)
        ];

        // Initialize Main fields from service or defaults
        var mainInfo = _dbConnectionService.MainDbConnectionInfo;
        MainServer = mainInfo.Server;
        SelectedMainDbAuthenticationMode = AuthenticationModeOption.From(mainInfo.Authentication);
        MainLogin = mainInfo.Login;
        MainPassword = mainInfo.Password;
        MainDatabase = mainInfo.Database;
        MainApplicationIntent = mainInfo.ApplicationIntent;
        MainEncryptOption = mainInfo.EncryptOption;
        MainTrustServerCertificate = mainInfo.TrustServerCertificate;
        MainAdditionalParameters = mainInfo.AdditionalParameters;

        // Initialize Param fields from service or defaults
        var paramInfo = _dbConnectionService.ParamDbConnectionInfo;
        ParamServer = paramInfo.Server;
        SelectedParamDbAuthenticationMode = AuthenticationModeOption.From(paramInfo.Authentication);
        ParamLogin = paramInfo.Login;
        ParamPassword = paramInfo.Password;
        ParamDatabase = paramInfo.Database;
        ParamApplicationIntent = paramInfo.ApplicationIntent;
        ParamEncryptOption = paramInfo.EncryptOption;
        ParamTrustServerCertificate = paramInfo.TrustServerCertificate;
        ParamAdditionalParameters = paramInfo.AdditionalParameters;

        ShareDbSettings = _dbConnectionService.ShareDbSettings;
    }

    /// <summary>
    /// Reconstructs DbConnectionInfo records from the observable properties and
    /// persists them to the connection service.
    /// </summary>
    public void ApplyChanges()
    {
        var mainDbConnectionInfo = new DbConnectionInfo
        {
            Server = MainServer,
            Authentication = SelectedMainDbAuthenticationMode.Mode,
            Login = MainLogin,
            Password = MainPassword,
            Database = MainDatabase,
            ApplicationIntent = MainApplicationIntent,
            EncryptOption = MainEncryptOption,
            TrustServerCertificate = MainTrustServerCertificate,
            AdditionalParameters = MainAdditionalParameters
        };
        _dbConnectionService.UpdateMainDbConnectionInfo(mainDbConnectionInfo);

        if (ShareDbSettings)
        {
            _dbConnectionService.UpdateParamDbConnectionInfo(mainDbConnectionInfo);
        }
        else
        {
            var paramDbConnectionInfo = new DbConnectionInfo
            {
                Server = ParamServer,
                Authentication = SelectedParamDbAuthenticationMode.Mode,
                Login = ParamLogin,
                Password = ParamPassword,
                Database = ParamDatabase,
                ApplicationIntent = ParamApplicationIntent,
                EncryptOption = ParamEncryptOption,
                TrustServerCertificate = ParamTrustServerCertificate,
                AdditionalParameters = ParamAdditionalParameters
            };
            _dbConnectionService.UpdateParamDbConnectionInfo(paramDbConnectionInfo);
        }

        _dbConnectionService.ShareDbSettings = ShareDbSettings;
    }
}