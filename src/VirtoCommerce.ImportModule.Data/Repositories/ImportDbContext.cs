using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;

namespace VirtoCommerce.ImportModule.Data.Repositories
{
    public class ImportDbContext : DbContextWithTriggers
    {
        public ImportDbContext(DbContextOptions<ImportDbContext> options)
          : base(options)
        {
        }

        protected ImportDbContext(DbContextOptions options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //        modelBuilder.Entity<ImportEntity>().ToTable("MyModule").HasKey(x => x.Id);
            //        modelBuilder.Entity<ImportEntity>().Property(x => x.Id).HasMaxLength(128);
            //        base.OnModelCreating(modelBuilder);
        }
    }
}

