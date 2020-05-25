﻿// <auto-generated />
using System;
using Database.DatabaseClasses;
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

            modelBuilder.Entity("Database.DatabaseClasses.Demo", b =>
                {
                    b.Property<long>("MatchId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<int?>("AnalysisBlockReason")
                        .HasColumnType("int");

                    b.Property<bool>("AnalysisSucceeded")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("BlobUrl")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("DownloadUrl")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<byte>("FileStatus")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte>("FramesPerSecond")
                        .HasColumnType("tinyint unsigned");

                    b.Property<string>("MD5Hash")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<DateTime>("MatchDate")
                        .HasColumnType("datetime(6)");

                    b.Property<byte>("Quality")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte>("Source")
                        .HasColumnType("tinyint unsigned");

                    b.Property<DateTime>("UploadDate")
                        .HasColumnType("datetime(6)");

                    b.Property<byte>("UploadType")
                        .HasColumnType("tinyint unsigned");

                    b.Property<long>("UploaderId")
                        .HasColumnType("bigint");

                    b.HasKey("MatchId");

                    b.ToTable("Demo");
                });

            modelBuilder.Entity("Database.DatabaseClasses.InQueueDemo", b =>
                {
                    b.Property<long>("MatchId")
                        .HasColumnType("bigint");

                    b.Property<byte>("CurrentQueue")
                        .HasColumnType("tinyint unsigned");

                    b.Property<int>("RetryAttemptsOnCurrentFailure")
                        .HasColumnType("int");

                    b.HasKey("MatchId");

                    b.ToTable("InQueueDemo");
                });

            modelBuilder.Entity("Database.DatabaseClasses.InQueueDemo", b =>
                {
                    b.HasOne("Database.DatabaseClasses.Demo", "Demo")
                        .WithOne("InQueueDemo")
                        .HasForeignKey("Database.DatabaseClasses.InQueueDemo", "MatchId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
