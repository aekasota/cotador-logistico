using System.Data;
using CotadorLogistico.Infrastructure.Database;

namespace CotadorLogistico.Tests.Database;

public sealed class DapperDateOnlyTypeHandlerTests
{
#pragma warning disable CS8766, CS8767
    private sealed class FakeParameter : IDbDataParameter
    {
        public DbType DbType { get; set; }
        public object? Value { get; set; }

        public ParameterDirection Direction { get; set; }
        public bool IsNullable => true;
        public string ParameterName { get; set; } = "";
        public string SourceColumn { get; set; } = "";
        public DataRowVersion SourceVersion { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
    }
#pragma warning restore CS8766, CS8767

    [Fact]
    public void SetValue_GravaComoDateTimeAMeiaNoiteEDbTypeDate()
    {
        var handler = new DapperDateOnlyTypeHandler();
        var parameter = new FakeParameter();

        handler.SetValue(parameter, new DateOnly(2026, 9, 15));

        Assert.Equal(DbType.Date, parameter.DbType);
        Assert.Equal(new DateTime(2026, 9, 15, 0, 0, 0), parameter.Value);
    }

    [Fact]
    public void Parse_ApartirDeDateTime_DevolveODateOnlyCorreto()
    {
        var handler = new DapperDateOnlyTypeHandler();

        var result = handler.Parse(new DateTime(2026, 9, 15, 14, 30, 0));

        Assert.Equal(new DateOnly(2026, 9, 15), result);
    }

    [Fact]
    public void RegisterOnce_NaoLancaAoSerChamadoMaisDeUmaVez()
    {
        DapperTypeHandlers.RegisterOnce();
        DapperTypeHandlers.RegisterOnce();
    }
}
