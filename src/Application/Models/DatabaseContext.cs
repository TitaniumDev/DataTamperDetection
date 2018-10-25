using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Application.Models
{
    public partial class DatabaseContext : DbContext
    {
        public DatabaseContext()
        {
        }

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public virtual DbSet<ThirdPartyPublicKeys> ThirdPartyPublicKeys { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename='C:\\SCC\\Scratch\\Data Change Detection\\Application\\Database\\Database.mdf';Integrated Security=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ThirdPartyPublicKeys>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.Property(e => e.Key).IsRequired();

                entity.Property(e => e.Signature)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(100);
            });
        }
    }
}
