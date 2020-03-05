using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pomelo.EntityFrameworkCore.MySql;
using RabbitCommunicationLib.Enums;
using DataBase.Enumerals;

namespace DataBase.DatabaseClasses
{
    public partial class DemoCentralContext : DbContext
    {
        public DemoCentralContext()
        {
        }

        public DemoCentralContext(DbContextOptions<DemoCentralContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Demo> Demo { get; set; }
        public virtual DbSet<InQueueDemo> InQueueDemo { get; set; }
        public virtual DbSet<Migrationhistory> MigrationHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.4-servicing-10062");

            modelBuilder.Entity<InQueueDemo>(entity =>
            {
                entity.HasKey(e => e.MatchId);
                entity.ToTable("InQueue");
            });

            modelBuilder.Entity<Demo>(entity =>
            {
                entity.HasKey(e => e.MatchId);
                entity.ToTable("Demo");

                //WORKAROUND This ensures the column is added as auto increment
                //Related https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1015
                //https://docs.microsoft.com/en-us/ef/core/modeling/generated-properties?tabs=data-annotations
                entity.Property(e => e.MatchId).HasColumnType("bigint(20) auto_increment").ValueGeneratedOnAdd();

                entity.Property(e => e.DownloadUrl).HasColumnType("longtext");

                entity.Property(e => e.BlobUrl).HasColumnType("longtext");

                entity.Property(e => e.Md5hash).HasColumnType("longtext");

                entity.Property(e => e.UploaderId).HasColumnType("bigint(20)");
            });

            modelBuilder.Entity<Migrationhistory>(entity =>
            {
                entity.HasKey(e => e.MigrationId);

                entity.ToTable("__efmigrationhistory");

                entity.Property(e => e.MigrationId)
                    .HasMaxLength(150)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.ContextKey)
                    .IsRequired()
                    .HasMaxLength(300)
                    .IsUnicode(false);

                entity.Property(e => e.Model)
                    .IsRequired()
                    .HasColumnType("longblob");

                entity.Property(e => e.ProductVersion)
                    .IsRequired()
                    .HasMaxLength(32)
                    .IsUnicode(false);
            });
        }
    }
}
