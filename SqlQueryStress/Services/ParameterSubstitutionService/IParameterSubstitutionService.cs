using System.Collections.Generic;

namespace SqlQueryStress.Services.ParameterSubstitutionService;

public interface IParameterSubstitutionService
{
    string ParameterQuery { get; set; }
    Dictionary<string, ParameterMappingEntry> ParameterMappings { get; set; }
}

public record ParameterMappingEntry(string DataType, string ColumnName);