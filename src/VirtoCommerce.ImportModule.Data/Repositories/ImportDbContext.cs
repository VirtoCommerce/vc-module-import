using System.Collections.Generic;
using System.Reflection;
using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VirtoCommerce.ImportModule.Data.Models;

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
            modelBuilder.Entity<ImportProfileEntity>().HasIndex(x => new { x.UserId, x.Name });

            #endregion

            #region ImportRunHistoryEntity

            modelBuilder.Entity<ImportRunHistoryEntity>().ToTable("ImportRunHistory").HasKey(x => x.Id);
            modelBuilder.Entity<ImportRunHistoryEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
            modelBuilder.Entity<ImportRunHistoryEntity>().Property(x => x.Errors)
            .HasConversion(y => JsonConvert.SerializeObject(y), y => JsonConvert.DeserializeObject<ICollection<string>>(y));
            modelBuilder.Entity<ImportRunHistoryEntity>().HasIndex(x => x.ProfileId);
            modelBuilder.Entity<ImportRunHistoryEntity>().HasIndex(x => x.UserId);
            modelBuilder.Entity<ImportRunHistoryEntity>().HasIndex(x => x.JobId);

            #endregion

            base.OnModelCreating(modelBuilder);

            // Allows configuration for an entity type for different database types.
            // Applies configuration from all <see cref="IEntityTypeConfiguration{TEntity}" in VirtoCommerce.ImportModule.Data.XXX project. /> 
            switch (Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.ImportModule.Data.MySql"));
                    break;
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.ImportModule.Data.PostgreSql"));
                    break;
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.ImportModule.Data.SqlServer"));
                    break;
            }
        }
    }
}

