using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VirtoCommerce.ImportModule.Data.Repositories;

namespace VirtoCommerce.ImportModule.Data.MySql;

public class MySqlDbContextFactory : IDesignTimeDbContextFactory<ImportDbContext>
{
    public ImportDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ImportDbContext>();
        var connectionString = args.Any() ? args[0] : "server=localhost;user=root;password=virto;database=VirtoCommerce3;";
        var serverVersion = args.Length >= 2 ? args[1] : null;

        builder.UseMySql(
            connectionString,
            ResolveServerVersion(serverVersion, connectionString),
            db => db.MigrationsAssembly(typeof(MySqlDbContextFactory).Assembly.GetName().Name));

        return new ImportDbContext(builder.Options);
    }

    private static ServerVersion ResolveServerVersion(string? serverVersion, string connectionString)
    {
        if (serverVersion == "AutoDetect")
        {
            return ServerVersion.AutoDetect(connectionString);
        }

        if (serverVersion != null)
        {
            return ServerVersion.Parse(serverVersion);
        }

        return new MySqlServerVersion(new Version(5, 7));
    }
}
