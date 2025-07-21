using Microsoft.EntityFrameworkCore;
using Orchestrator.Repository.Model;

namespace Booking.Repository;

public class BookingDbContext(DbContextOptions<BookingDbContext> dbContextOptions) : DbContext(dbContextOptions)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingItem>().HasKey(x => x.Id);
        modelBuilder.Entity<BookingItem>().Property(e => e.Id).ValueGeneratedOnAdd();

    }

    public DbSet<BookingItem> Bookings { get; set; }
}
