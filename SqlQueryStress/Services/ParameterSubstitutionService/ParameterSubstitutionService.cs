using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Models;
using SqlQueryStress.Services.DbConnectionService;

namespace SqlQueryStress.Services.ParameterSubstitutionService;

public class ParameterSubstitutionService : IParameterSubstitutionService
{
    private static readonly Regex ParameterRegex = new(@"(?<=[=,\s\(])@\w{1,}(?=[=,\s\)]?)");
    public string ParameterQuery { get; set; } = "";
    public Dictionary<string, ParameterMappingEntry> ParameterMappings { get; set; } = [];

    public string[] ExtractParametersFromQuery(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var parameters = new List<string>();

        foreach (Match match in ParameterRegex.Matches(query))
        {
            var parameter = match.Value.ToLowerInvariant();

            if (!parameters.Contains(parameter))
                parameters.Add(parameter);
        }

        return parameters.ToArray();
    }

    public async Task<IReadOnlyList<ParameterColumn>> GetParameterColumnsAsync
    (
        string parameterQuery,
        DbConnectionInfo dbConnectionInfo,
        int connectionTimeout
    ) {
        ArgumentNullException.ThrowIfNull(parameterQuery);
        ArgumentNullException.ThrowIfNull(dbConnectionInfo);

        if (string.IsNullOrWhiteSpace(parameterQuery))
            return [];

        var connectionString =
            ConnectionStringBuilder.Build(dbConnectionInfo, connectionTimeout);

        await using var connection = new SqlConnection(connectionString);
        ConnectionSettingsHelper.AttachEventHandler(connection);

        await connection.OpenAsync();

        await using var command = new SqlCommand(parameterQuery, connection);

        await using var reader =
            await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly);

        var columns = new List<ParameterColumn>();

        for (var i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(
                new ParameterColumn(
                    reader.GetName(i),
                    reader.GetDataTypeName(i)));
        }

        return columns;
    }
}