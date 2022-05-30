using System.Collections.Generic;
using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VirtoCommerce.ImportModule.Data.Infrastructure.DataEntities;

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
            #region ImportProfileEntity
            modelBuilder.Entity<ImportProfileEntity>().ToTable("ImportProfile").HasKey(x => x.Id);
            modelBuilder.Entity<ImportProfileEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
            modelBuilder.Entity<ImportProfileEntity>().HasIndex(x => new { x.SellerId, x.Name });
            #endregion

            #region ImportRunHistoryEntity
            modelBuilder.Entity<ImportRunHistoryEntity>().ToTable("ImportRunHistory").HasKey(x => x.Id);
            modelBuilder.Entity<ImportRunHistoryEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
            modelBuilder.Entity<ImportRunHistoryEntity>().Property(x => x.Errors)
            .HasConversion(y => JsonConvert.SerializeObject(y), y => JsonConvert.DeserializeObject<ICollection<string>>(y));
            #endregion
        }
    }
}

