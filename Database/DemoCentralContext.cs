using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pomelo.EntityFrameworkCore.MySql;
using RabbitCommunicationLib.Enums;
using Database.Enumerals;

namespace Database.DatabaseClasses
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.4-servicing-10062");

            modelBuilder.Entity<InQueueDemo>(entity =>
            {
                entity.HasKey(e => e.MatchId);
            });

            modelBuilder.Entity<Demo>(entity =>
            {
                entity.HasKey(e => e.MatchId);                    

                entity.Property(e => e.MatchId).ValueGeneratedOnAdd();
            });


            // Setup Navigation Properties for Demo and InQueueDemo Tables.
            modelBuilder.Entity<InQueueDemo>(entity =>
            {
                entity.HasKey(e => e.MatchId);

                // One to zero/one relation
                // Every InQueueDemo has a Demo, but not every Demo has an InQueueDemo
                entity.HasOne(d => (Demo)d.Demo)
                    .WithOne(p => p.InQueueDemo)
                    .HasForeignKey<InQueueDemo>(d => d.MatchId)
                    .IsRequired();
            });
        }
    }
}
