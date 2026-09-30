using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Services.DbConnectionService;
using SqlQueryStress.Services.ParameterSubstitutionService;

namespace SqlQueryStress.Services.LoadEngine;

internal sealed class ParamServer
{
    // Matches SQL type names with optional size/precision and scale.
    // Examples:
    //   int
    //   varchar(50)
    //   nvarchar(max)
    //   decimal(18,2)
    //
    // Capture groups:
    //   1 -> base type name
    //   2 -> size or precision
    //   3 -> scale
    private static readonly Regex TypeNameRegex = new(
        @"^(\w+)(?:\((\d+|-1|max)(?:,(\d+))?\))?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private readonly SqlParameter[] _parameterTemplates;
    private readonly object?[][] _rows;

    // start at -1 so the first increment selects row 0
    private int _currentRow = -1;

    private ParamServer(
        SqlParameter[] parameterTemplates,
        object?[][] rows
    )
    {
        _parameterTemplates = parameterTemplates;
        _rows = rows;
    }

    public static async Task<ParamServer> CreateAsync(
        string parameterQuery,
        string connectionString,
        IReadOnlyDictionary<string, ParameterMappingEntry> mappings,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterQuery);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(mappings);

        // Do not mutate the mappings owned by ParameterSubstitutionService
        // Only mappings with a selected source column participate in a run

        var activeMappings = mappings
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.Value.ColumnName))
            .ToArray();

        if (activeMappings.Length == 0)
            throw new ArgumentException(
                "At least one parameter must be mapped to a column.",
                nameof(mappings));

        await using var connection = new SqlConnection(connectionString);
        ConnectionSettingsHelper.AttachEventHandler(connection);

        await connection.OpenAsync(cancellationToken);

#pragma warning disable CA2100 // parameter queries are user supplied
        await using var command = new SqlCommand(parameterQuery, connection);
#pragma warning restore CA2100

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var parameterTemplates = new SqlParameter[activeMappings.Length];
        var columnOrdinals = new int[activeMappings.Length];

        for (var i = 0; i < activeMappings.Length; i++)
        {
            var mapping = activeMappings[i];

            parameterTemplates[i] = new SqlParameter
            {
                ParameterName = mapping.Key
            };

            ApplyTypeToParameter(
                parameterTemplates[i],
                mapping.Value.DataType
            );

            try
            {
                columnOrdinals[i] = reader.GetOrdinal(mapping.Value.ColumnName);
            }
            catch (IndexOutOfRangeException exception)
            {
                throw new InvalidOperationException(
                    $"The parameter query did not return the mapped column " +
                    $"'{mapping.Value.ColumnName}' for parameter " +
                    $"'{mapping.Key}'.",
                    exception);
            }
        }

        var rows = new List<object?[]>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new object?[activeMappings.Length];

            for (var i = 0; i < columnOrdinals.Length; i++)
            {
                var ordinal = columnOrdinals[i];

                row[i] = reader.IsDBNull(ordinal)
                            ? DBNull.Value
                            : reader.GetValue(ordinal);
            }

            rows.Add(row);
        }

        if (rows.Count == 0)
            throw new InvalidOperationException(
                "The parameter query returned no rows"
            );

        return new ParamServer(parameterTemplates, rows.ToArray());
    }

    public SqlParameter[] CreateParameters()
    {
        var parameters = new SqlParameter[_parameterTemplates.Length];

        for (var i = 0; i < _parameterTemplates.Length; i++)
            parameters[i] = (SqlParameter)((ICloneable)_parameterTemplates[i]).Clone();

        return parameters;
    }

    public void PopulateNextRow(
        SqlParameterCollection parameters
    )
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.Count != _parameterTemplates.Length)
            throw new InvalidOperationException(
                "The parameter collection does not match the configured mappings"
            );

        var rowNumber = Interlocked.Increment(ref _currentRow);

        // Casting to uint also keeps rotation valid if the integer eventually
        // overflows during a really long-running stress test
        var rowIndex = (int)((uint)rowNumber % (uint)_rows.Length);
        var rows = _rows[rowIndex];

        for (var i = 0; i < rows.Length; i++)
            parameters[i].Value = rows[i] ?? DBNull.Value;
    }

    private static void ApplyTypeToParameter(
        SqlParameter parameter,
        string? typeName
    )
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return;

        var match = TypeNameRegex.Match(typeName.Trim());

        if (!match.Success)
            return;

        var baseType = match.Groups[1].Value.ToUpperInvariant();
        var sizeOrPrecision = ParseSize(match.Groups[2].Value);
        var scale = ParseScale(match.Groups[3].Value);

        switch (baseType)
        {
            case "NVARCHAR":
                parameter.SqlDbType = SqlDbType.NVarChar;
                SetSize(parameter, sizeOrPrecision);
                break;

            case "VARCHAR":
                parameter.SqlDbType = SqlDbType.VarChar;
                SetSize(parameter, sizeOrPrecision);
                break;

            case "NCHAR":
                parameter.SqlDbType = SqlDbType.NChar;
                SetSize(parameter, sizeOrPrecision);
                break;

            case "CHAR":
                parameter.SqlDbType = SqlDbType.Char;
                SetSize(parameter, sizeOrPrecision);
                break;

            case "INT":
                parameter.SqlDbType = SqlDbType.Int;
                break;

            case "BIGINT":
                parameter.SqlDbType = SqlDbType.BigInt;
                break;

            case "SMALLINT":
                parameter.SqlDbType = SqlDbType.SmallInt;
                break;

            case "TINYINT":
                parameter.SqlDbType = SqlDbType.TinyInt;
                break;

            case "BIT":
                parameter.SqlDbType = SqlDbType.Bit;
                break;

            case "DECIMAL":
            case "NUMERIC":
                parameter.SqlDbType = SqlDbType.Decimal;

                if (sizeOrPrecision.HasValue)
                {
                    parameter.Precision =
                        checked((byte)sizeOrPrecision.Value);
                }

                if (scale.HasValue)
                    parameter.Scale = scale.Value;

                break;

            case "FLOAT":
                parameter.SqlDbType = SqlDbType.Float;
                break;

            case "REAL":
                parameter.SqlDbType = SqlDbType.Real;
                break;

            case "DATETIME":
                parameter.SqlDbType = SqlDbType.DateTime;
                break;

            case "DATETIME2":
                parameter.SqlDbType = SqlDbType.DateTime2;
                break;

            case "DATE":
                parameter.SqlDbType = SqlDbType.Date;
                break;

            case "TIME":
                parameter.SqlDbType = SqlDbType.Time;
                break;

            case "UNIQUEIDENTIFIER":
                parameter.SqlDbType = SqlDbType.UniqueIdentifier;
                break;

            case "XML":
                parameter.SqlDbType = SqlDbType.Xml;
                break;

            case "MONEY":
                parameter.SqlDbType = SqlDbType.Money;
                break;

            case "SMALLMONEY":
                parameter.SqlDbType = SqlDbType.SmallMoney;
                break;

            case "VARBINARY":
                parameter.SqlDbType = SqlDbType.VarBinary;
                SetSize(parameter, sizeOrPrecision);
                break;

            case "BINARY":
                parameter.SqlDbType = SqlDbType.Binary;
                SetSize(parameter, sizeOrPrecision);
                break;
        }
    }

    private static int? ParseSize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (value.Equals("max", StringComparison.OrdinalIgnoreCase))
            return -1;

        return int.TryParse(value, out var size)
            ? size
            : (int?)null;
    }

    private static byte? ParseScale(string value)
    {
        return byte.TryParse(value, out var scale)
            ? scale
            : null;
    }

    private static void SetSize(
        SqlParameter parameter,
        int? size
    )
    {
        if(size.HasValue)
            parameter.Size = size.Value;
    }
}
