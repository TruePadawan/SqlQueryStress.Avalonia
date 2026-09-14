using SqlQueryStress.Models;

namespace SqlQueryStress.DTOs;

public class DbConnectionInfoDto
{
    public string AdditionalParameters { get; set; } = "";
    public int ApplicationIntent { get; set; }
    public bool AzureMFA { get; set; }
    public int ConnectTimeout { get; set; }
    public string Database { get; set; } = "";
    public bool EnablePooling { get; set; } = true;
    public string EncryptOption { get; set; } = "False";
    public bool IntegratedAuth { get; set; } = true;
    public string Login { get; set; } = "";
    public int MaxPoolSize { get; set; } = 2;
    public string Password { get; set; } = "";
    public string Server { get; set; } = "";
    public bool TrustServerCertificate { get; set; }

    public static DbConnectionInfoDto FromModel(DbConnectionInfo model, int globalConnectionTimeout)
    {
        return new DbConnectionInfoDto
        {
            Server = model.Server,
            IntegratedAuth = model.Authentication == AuthenticationMode.Integrated,
            AzureMFA = model.Authentication == AuthenticationMode.AzureMFA,
            Login = model.Login,
            Password = model.Password,
            Database = model.Database,
            ApplicationIntent = (int)model.ApplicationIntent,
            EncryptOption = model.EncryptOption.ToString(),
            TrustServerCertificate = model.TrustServerCertificate,
            AdditionalParameters = model.AdditionalParameters,
            EnablePooling = model.EnablePooling,
            MaxPoolSize = model.MaxPoolSize,
            ConnectTimeout = globalConnectionTimeout
        };
    }

    public static DbConnectionInfo ToModel(DbConnectionInfoDto dto)
    {
        var authMode = dto.AzureMFA ? AuthenticationMode.AzureMFA :
            dto.IntegratedAuth ? AuthenticationMode.Integrated :
            AuthenticationMode.SqlServer;

        var appIntent = System.Enum.IsDefined(typeof(ApplicationIntent), dto.ApplicationIntent)
            ? (ApplicationIntent)dto.ApplicationIntent
            : SqlQueryStress.Models.ApplicationIntent.ReadWrite;

        var encryptOption = System.Enum.TryParse<EncryptOption>(dto.EncryptOption, true, out var enc)
            ? enc
            : SqlQueryStress.Models.EncryptOption.False;

        return new DbConnectionInfo
        {
            Server = dto.Server,
            Authentication = authMode,
            Login = dto.Login,
            Password = dto.Password,
            Database = dto.Database,
            ApplicationIntent = appIntent,
            EncryptOption = encryptOption,
            TrustServerCertificate = dto.TrustServerCertificate,
            AdditionalParameters = dto.AdditionalParameters,
            EnablePooling = dto.EnablePooling,
            MaxPoolSize = dto.MaxPoolSize > 0 ? dto.MaxPoolSize : 2
        };
    }
}