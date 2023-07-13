using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VirtoCommerce.ImportModule.Data.Repositories;

namespace VirtoCommerce.ImportModule.Data.PostgreSql;

public class PostgreSqlDbContextFactory : IDesignTimeDbContextFactory<ImportDbContext>
{
    public ImportDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ImportDbContext>();
        var connectionString = args.Any() ? args[0] : "User ID = postgres; Password = password; Host = localhost; Port = 5432; Database = virtocommerce3;";

        builder.UseNpgsql(
            connectionString,
            db => db.MigrationsAssembly(typeof(PostgreSqlDbContextFactory).Assembly.GetName().Name));

        return new ImportDbContext(builder.Options);
    }
}
