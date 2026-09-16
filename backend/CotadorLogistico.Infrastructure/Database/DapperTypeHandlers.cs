using System.Data;
using Dapper;

namespace CotadorLogistico.Infrastructure.Database;

public sealed class DapperDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly dateOnly => dateOnly,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => throw new InvalidCastException($"Não foi possível converter {value.GetType()} para DateOnly.")
    };
}

public static class DapperTypeHandlers
{
    private static bool _registered;

    public static void RegisterOnce()
    {
        if (_registered) return;
        SqlMapper.AddTypeHandler(new DapperDateOnlyTypeHandler());
        _registered = true;
    }
}
