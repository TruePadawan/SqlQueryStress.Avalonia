using System.Collections.Generic;

namespace SqlQueryStress.Services.ParameterSubstitutionService;

public class ParameterSubstitutionService : IParameterSubstitutionService
{
    public string ParameterQuery { get; set; } = "";
    public Dictionary<string, ParameterMappingEntry> ParameterMappings { get; set; } = [];
}