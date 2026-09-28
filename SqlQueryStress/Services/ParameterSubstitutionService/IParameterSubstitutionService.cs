using System.Collections.Generic;
using System.Threading.Tasks;
using SqlQueryStress.Models;

namespace SqlQueryStress.Services.ParameterSubstitutionService;

public interface IParameterSubstitutionService
{
    string ParameterQuery { get; set; }
    Dictionary<string, ParameterMappingEntry> ParameterMappings { get; set; }

    string[] ExtractParametersFromQuery(string query);

    Task<IReadOnlyList<ParameterColumn>> GetParameterColumnsAsync(
        string parameterQuery,
        DbConnectionInfo dbConnectionInfo,
        int connectionTimeout 
    );
}

public record ParameterMappingEntry(string DataType, string ColumnName);

public record ParameterColumn(string Name, string DataType);