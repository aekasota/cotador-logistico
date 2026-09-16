using Npgsql;

namespace CotadorLogistico.Infrastructure.Database;

public static class NpgsqlDataSourceFactory
{
    public static NpgsqlDataSource Create(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.ConnectionStringBuilder.Timeout = 10;
        builder.ConnectionStringBuilder.CommandTimeout = 30;
        return builder.Build();
    }
}
