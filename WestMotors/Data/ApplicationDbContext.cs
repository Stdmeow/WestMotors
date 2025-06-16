using WestMotorsApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WestMotorsApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Car> Cars { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Deal> Deals { get; set; }
        public DbSet<AutoServiceEntry> AutoServiceEntries { get; set; }
        public DbSet<SparePart> SpareParts { get; set; }
        public DbSet<UsedPartInService> UsedPartsInService { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<ApplicationRequest> ApplicationRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UsedPartInService>()
                .HasKey(ups => new { ups.ServiceId, ups.PartId });

            modelBuilder.Entity<UsedPartInService>()
                .HasOne(ups => ups.AutoServiceEntry)
                .WithMany(ase => ase.UsedParts)
                .HasForeignKey(ups => ups.ServiceId);

            modelBuilder.Entity<UsedPartInService>()
                .HasOne(ups => ups.SparePart)
                .WithMany(sp => sp.UsedInServices)
                .HasForeignKey(ups => ups.PartId);

            modelBuilder.Entity<Car>()
                .HasMany(c => c.Deals)
                .WithOne(d => d.Car)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.Restrict);

            // ИЗМЕНЕНИЕ ЗДЕСЬ: Добавляем .IsRequired(false)
            modelBuilder.Entity<Deal>()
                .HasOne(d => d.Seller)
                .WithMany()
                .HasForeignKey(d => d.SellerId)
                .IsRequired(false) // <--- ВОТ ЭТА СТРОКА ГОВОРИТ, ЧТО SellerId МОЖЕТ БЫТЬ NULL
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AutoServiceEntry>()
                .HasOne(ase => ase.Mechanic)
                .WithMany()
                .HasForeignKey(ase => ase.MechanicId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Car>()
                .HasIndex(c => c.VIN)
                .IsUnique();
        }
    }
}