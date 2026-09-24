using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlQueryStress.Services.ParameterSubstitutionService;

public class ParameterSubstitutionService : IParameterSubstitutionService
{
    private static readonly Regex ParameterRegex = new (@"(?<=[=,\s\(])@\w{1,}(?=[=,\s\)]?)");
    public string ParameterQuery { get; set; } = "";
    public Dictionary<string, ParameterMappingEntry> ParameterMappings { get; set; } = [];

    public string[] ExtractParametersFromQuery(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var parameters = new List<string>();

        foreach (Match match in ParameterRegex.Matches(query))
        {
            var parameter = match.Value.ToLowerInvariant();
            
            if(!parameters.Contains(parameter))
                parameters.Add(parameter);
        }
        
        return parameters.ToArray();
    }
}