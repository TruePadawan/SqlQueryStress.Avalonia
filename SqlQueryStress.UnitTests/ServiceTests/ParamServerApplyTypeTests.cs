using System.Data;
using System.Reflection;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Services.LoadEngine;

namespace SqlQueryStress.UnitTests.ServiceTests;

public class ParamServerApplyTypeTests
{
    private static readonly MethodInfo ApplyTypeMethod = typeof(ParamServer)
        .GetMethod("ApplyTypeToParameter", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void ApplyType(SqlParameter parameter, string? typeName)
    {
        ApplyTypeMethod.Invoke(null, [parameter, typeName]);
    }

    #region String types with size

    [Fact]
    public void ApplyType_NVARCHAR50_SetsNVarCharAndSize()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "NVARCHAR(50)");

        p.SqlDbType.Should().Be(SqlDbType.NVarChar);
        p.Size.Should().Be(50);
    }

    [Fact]
    public void ApplyType_VARCHAR100_SetsVarCharAndSize()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "VARCHAR(100)");

        p.SqlDbType.Should().Be(SqlDbType.VarChar);
        p.Size.Should().Be(100);
    }

    [Fact]
    public void ApplyType_NCHAR10_SetsNCharAndSize()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "NCHAR(10)");

        p.SqlDbType.Should().Be(SqlDbType.NChar);
        p.Size.Should().Be(10);
    }

    [Fact]
    public void ApplyType_CHAR20_SetsCharAndSize()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "CHAR(20)");

        p.SqlDbType.Should().Be(SqlDbType.Char);
        p.Size.Should().Be(20);
    }

    [Fact]
    public void ApplyType_NVARCHAR_max_SetsNVarCharAndSizeMinus1()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "NVARCHAR(max)");

        p.SqlDbType.Should().Be(SqlDbType.NVarChar);
        p.Size.Should().Be(-1);
    }

    #endregion

    #region Numeric types

    [Fact]
    public void ApplyType_INT_SetsInt()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "INT");

        p.SqlDbType.Should().Be(SqlDbType.Int);
    }

    [Fact]
    public void ApplyType_BIGINT_SetsBigInt()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "BIGINT");

        p.SqlDbType.Should().Be(SqlDbType.BigInt);
    }

    [Fact]
    public void ApplyType_SMALLINT_SetsSmallInt()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "SMALLINT");

        p.SqlDbType.Should().Be(SqlDbType.SmallInt);
    }

    [Fact]
    public void ApplyType_TINYINT_SetsTinyInt()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "TINYINT");

        p.SqlDbType.Should().Be(SqlDbType.TinyInt);
    }

    [Fact]
    public void ApplyType_BIT_SetsBit()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "BIT");

        p.SqlDbType.Should().Be(SqlDbType.Bit);
    }

    [Fact]
    public void ApplyType_DECIMAL18_2_SetsDecimalWithPrecisionAndScale()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "DECIMAL(18,2)");

        p.SqlDbType.Should().Be(SqlDbType.Decimal);
        p.Precision.Should().Be(18);
        p.Scale.Should().Be(2);
    }

    [Fact]
    public void ApplyType_NUMERIC10_4_SetsDecimalWithPrecisionAndScale()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "NUMERIC(10,4)");

        p.SqlDbType.Should().Be(SqlDbType.Decimal);
        p.Precision.Should().Be(10);
        p.Scale.Should().Be(4);
    }

    [Fact]
    public void ApplyType_FLOAT_SetsFloat()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "FLOAT");

        p.SqlDbType.Should().Be(SqlDbType.Float);
    }

    [Fact]
    public void ApplyType_REAL_SetsReal()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "REAL");

        p.SqlDbType.Should().Be(SqlDbType.Real);
    }

    #endregion

    #region Date/time types

    [Fact]
    public void ApplyType_DATETIME_SetsDateTime()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "DATETIME");

        p.SqlDbType.Should().Be(SqlDbType.DateTime);
    }

    [Fact]
    public void ApplyType_DATETIME2_SetsDateTime2()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "DATETIME2");

        p.SqlDbType.Should().Be(SqlDbType.DateTime2);
    }

    [Fact]
    public void ApplyType_DATE_SetsDate()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "DATE");

        p.SqlDbType.Should().Be(SqlDbType.Date);
    }

    [Fact]
    public void ApplyType_TIME_SetsTime()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "TIME");

        p.SqlDbType.Should().Be(SqlDbType.Time);
    }

    #endregion

    #region Other types

    [Fact]
    public void ApplyType_UNIQUEIDENTIFIER_SetsUniqueIdentifier()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "UNIQUEIDENTIFIER");

        p.SqlDbType.Should().Be(SqlDbType.UniqueIdentifier);
    }

    [Fact]
    public void ApplyType_XML_SetsXml()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "XML");

        p.SqlDbType.Should().Be(SqlDbType.Xml);
    }

    [Fact]
    public void ApplyType_MONEY_SetsMoney()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "MONEY");

        p.SqlDbType.Should().Be(SqlDbType.Money);
    }

    [Fact]
    public void ApplyType_SMALLMONEY_SetsSmallMoney()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "SMALLMONEY");

        p.SqlDbType.Should().Be(SqlDbType.SmallMoney);
    }

    [Fact]
    public void ApplyType_VARBINARY8000_SetsVarBinaryAndSize()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "VARBINARY(8000)");

        p.SqlDbType.Should().Be(SqlDbType.VarBinary);
        p.Size.Should().Be(8000);
    }

    [Fact]
    public void ApplyType_BINARY50_SetsBinaryAndSize()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "BINARY(50)");

        p.SqlDbType.Should().Be(SqlDbType.Binary);
        p.Size.Should().Be(50);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void ApplyType_Null_NoChange()
    {
        var p = new SqlParameter("@p", SqlDbType.Int);
        ApplyType(p, null);

        p.SqlDbType.Should().Be(SqlDbType.Int);
    }

    [Fact]
    public void ApplyType_Empty_NoChange()
    {
        var p = new SqlParameter("@p", SqlDbType.Int);
        ApplyType(p, "");

        p.SqlDbType.Should().Be(SqlDbType.Int);
    }

    [Fact]
    public void ApplyType_Whitespace_NoChange()
    {
        var p = new SqlParameter("@p", SqlDbType.Int);
        ApplyType(p, "   ");

        p.SqlDbType.Should().Be(SqlDbType.Int);
    }

    [Fact]
    public void ApplyType_UnknownType_NoChange()
    {
        var p = new SqlParameter("@p", SqlDbType.Int);
        ApplyType(p, "SOME_UNKNOWN_TYPE");

        p.SqlDbType.Should().Be(SqlDbType.Int);
    }

    [Fact]
    public void ApplyType_CaseInsensitive_nvarchar50_SetsNVarChar()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "nvarchar(50)");

        p.SqlDbType.Should().Be(SqlDbType.NVarChar);
        p.Size.Should().Be(50);
    }

    [Fact]
    public void ApplyType_CaseInsensitive_int_SetsInt()
    {
        var p = new SqlParameter("@p", SqlDbType.VarChar);
        ApplyType(p, "int");

        p.SqlDbType.Should().Be(SqlDbType.Int);
    }

    #endregion
}