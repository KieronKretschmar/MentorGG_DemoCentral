using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pomelo.EntityFrameworkCore.MySql;
using RabbitTransfer.Enums;
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
                entity.ToTable("InQueue", "democentral");
            });

            modelBuilder.Entity<Demo>(entity =>
            {
                entity.HasKey(e => e.MatchId);
                entity.ToTable("Demo", "democentral");

                entity.Property(e => e.MatchId).HasColumnType("int(11)").ValueGeneratedOnAdd();

                entity.Property(e => e.DownloadUrl).HasColumnType("longtext");

                entity.Property(e => e.FileName).HasColumnType("longtext");

                entity.Property(e => e.FilePath).HasColumnType("longtext");

                //Ensure that an enum is stored as a byte inside the database, while still allowing for enum use in DB model
                //Otherwise the enum would fail
                entity.Property(e => e.FileStatus).HasColumnType("tinyint(3)").HasConversion<byte>();
                entity.Property(e => e.UploadType).HasColumnType("tinyint(3)").HasConversion<byte>();
                entity.Property(e => e.DemoFileWorkerStatus).HasColumnType("tinyint(3)").HasConversion<byte>();
                entity.Property(e => e.UploadStatus).HasColumnType("tinyint(3)").HasConversion<byte>();
                entity.Property(e => e.Source).HasColumnType("tinyint(3)").HasConversion<byte>();


                entity.Property(e => e.Md5hash)
                    .HasColumnType("longtext");

                entity.Property(e => e.UploaderId).HasColumnType("bigint(20)");
            });

            modelBuilder.Entity<Migrationhistory>(entity =>
            {
                entity.HasKey(e => e.MigrationId);

                entity.ToTable("__efmigrationhistory", "democentral");

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
