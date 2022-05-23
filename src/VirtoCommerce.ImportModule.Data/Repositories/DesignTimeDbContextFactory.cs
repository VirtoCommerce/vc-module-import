using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VirtoCommerce.ImportModule.Data.Repositories
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ImportDbContext>
    {
        public ImportDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ImportDbContext>();

            builder.UseSqlServer("Data Source=(local);Initial Catalog=VirtoCommerce3;Persist Security Info=True;User ID=virto;Password=virto;MultipleActiveResultSets=True;Connect Timeout=30");

            return new ImportDbContext(builder.Options);
        }
    }
}
