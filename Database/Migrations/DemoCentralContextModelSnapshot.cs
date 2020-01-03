﻿// <auto-generated />
using System;
using DataBase.DatabaseClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Database.Migrations
{
    [DbContext(typeof(DemoCentralContext))]
    partial class DemoCentralContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.4-servicing-10062")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("DataBase.DatabaseClasses.Demo", b =>
                {
                    b.Property<int>("MatchId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11)");

                    b.Property<byte>("DemoAnalyzerStatus")
                        .HasColumnType("tinyint(3) unsigned");

                    b.Property<string>("DemoAnalyzerVersion")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("DownloadUrl")
                        .HasColumnType("longtext");

                    b.Property<string>("Event")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("FileName")
                        .HasColumnType("longtext");

                    b.Property<string>("FilePath")
                        .HasColumnType("longtext");

                    b.Property<byte>("FileStatus")
                        .HasColumnType("tinyint(3) unsigned");

                    b.Property<DateTime>("MatchDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Md5hash")
                        .HasColumnName("MD5Hash")
                        .HasColumnType("longtext");

                    b.Property<byte>("Source")
                        .HasColumnType("tinyint(3) unsigned");

                    b.Property<DateTime>("UploadDate")
                        .HasColumnType("datetime(6)");

                    b.Property<byte>("UploadStatus")
                        .HasColumnType("tinyint(3) unsigned");

                    b.Property<byte>("UploadType")
                        .HasColumnType("tinyint(3) unsigned");

                    b.Property<long>("UploaderId")
                        .HasColumnType("bigint(20)");

                    b.HasKey("MatchId");

                    b.ToTable("Demo","democentral");
                });

            modelBuilder.Entity("DataBase.DatabaseClasses.InQueueDemo", b =>
                {
                    b.Property<long>("MatchId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<short>("DDQUEUE")
                        .HasColumnType("smallint");

                    b.Property<short>("DFWQUEUE")
                        .HasColumnType("smallint");

                    b.Property<DateTime>("InsertDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("MatchDate")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Retries")
                        .HasColumnType("int");

                    b.Property<short>("SOQUEUE")
                        .HasColumnType("smallint");

                    b.Property<long>("UploaderId")
                        .HasColumnType("bigint");

                    b.HasKey("MatchId");

                    b.ToTable("InQueue","democentral");
                });

            modelBuilder.Entity("DataBase.DatabaseClasses.Migrationhistory", b =>
                {
                    b.Property<string>("MigrationId")
                        .HasColumnType("varchar(150) CHARACTER SET utf8mb4")
                        .HasMaxLength(150)
                        .IsUnicode(false);

                    b.Property<string>("ContextKey")
                        .IsRequired()
                        .HasColumnType("varchar(300) CHARACTER SET utf8mb4")
                        .HasMaxLength(300)
                        .IsUnicode(false);

                    b.Property<byte[]>("Model")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<string>("ProductVersion")
                        .IsRequired()
                        .HasColumnType("varchar(32) CHARACTER SET utf8mb4")
                        .HasMaxLength(32)
                        .IsUnicode(false);

                    b.HasKey("MigrationId");

                    b.ToTable("__efmigrationhistory","democentral");
                });
#pragma warning restore 612, 618
        }
    }
}
