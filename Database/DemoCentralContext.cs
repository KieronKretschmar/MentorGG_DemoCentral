using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pomelo.EntityFrameworkCore.MySql;
using RabbitTransfer.Enums;

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

                entity.Property(e => e.UploaderId).HasColumnType("bigint(20)");
                entity.Property(e => e.Source).HasColumnType("tinyint(3) unsigned").HasConversion(new EnumToNumberConverter<Source,byte>());
                entity.Property(e => e.UploadType).HasColumnType("tinyint(3) unsigned").HasConversion(new EnumToNumberConverter<UploadType, byte>());

                entity.Property(e => e.DFWQUEUE).HasConversion(new BoolToZeroOneConverter<short>());
                entity.Property(e => e.SOQUEUE).HasConversion(new BoolToZeroOneConverter<short>());

            });

            modelBuilder.Entity<Demo>(entity =>
            {
                entity.HasKey(e => e.MatchId);
                entity.HasData(new Demo() { MatchId = 1, UploaderId = 1, Source = 1, DemoAnalyzerVersion = "" },
                    new Demo() { MatchId = 2, UploaderId = 2, Source = 1 ,DemoAnalyzerVersion = ""},
                    new Demo() { MatchId = 3, UploaderId = 3, Source = 2 , DemoAnalyzerVersion = ""});
                entity.ToTable("Demo", "democentral");

                entity.Property(e => e.MatchId).HasColumnType("int(11)").ValueGeneratedOnAdd();

                entity.Property(e => e.DemoAnalyzerStatus).HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.DownloadUrl).HasColumnType("longtext");

                entity.Property(e => e.FileName).HasColumnType("longtext");

                entity.Property(e => e.FilePath).HasColumnType("longtext");

                entity.Property(e => e.FileStatus).HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.Md5hash)
                    .HasColumnName("MD5Hash")
                    .HasColumnType("longtext");

                entity.Property(e => e.Source).HasColumnType("tinyint(3) unsigned").HasConversion(new EnumToNumberConverter<Source, byte>());

                entity.Property(e => e.UploadType).HasColumnType("tinyint(3) unsigned").HasConversion(new EnumToNumberConverter<UploadType, byte>());


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
