using System.Collections.Generic;

namespace SqlQueryStress.Services.ParameterSubstitutionService;

public interface IParameterSubstitutionService
{
    string ParameterQuery { get; set; }
    Dictionary<string, ParameterMappingEntry> ParameterMappings { get; set; }

    string[] ExtractParametersFromQuery(string query);
}

public record ParameterMappingEntry(string DataType, string ColumnName);