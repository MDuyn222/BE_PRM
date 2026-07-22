using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<DriverLicense> DriverLicenses => Set<DriverLicense>();
    public DbSet<IdentityVerification> IdentityVerifications => Set<IdentityVerification>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<BookingDriver> BookingDrivers => Set<BookingDriver>();
    public DbSet<VehicleReservationHold> VehicleReservationHolds => Set<VehicleReservationHold>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Promotion> Promotions => Set<Promotion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Role).HasMaxLength(30);
            e.Property(x => x.PhoneNumber).HasMaxLength(30);
            e.Property(x => x.IdentityNumber).HasMaxLength(30);
            e.Property(x => x.VerificationStatus).HasMaxLength(20);
        });

        modelBuilder.Entity<DriverLicense>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasIndex(x => x.LicenseNumber).IsUnique();
            e.Property(x => x.LicenseNumber).HasMaxLength(50);
            e.Property(x => x.LicenseType).HasMaxLength(30);
            e.Property(x => x.VerificationStatus).HasMaxLength(20);
            e.HasOne(x => x.User).WithOne(x => x.DriverLicense).HasForeignKey<DriverLicense>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdentityVerification>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasIndex(x => x.CCCDNumber).IsUnique();
            e.Property(x => x.CCCDNumber).HasMaxLength(30);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.User).WithOne(x => x.IdentityVerification).HasForeignKey<IdentityVerification>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasIndex(x => x.LicensePlate).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Type).HasMaxLength(30);
            e.Property(x => x.Brand).HasMaxLength(100);
            e.Property(x => x.Model).HasMaxLength(100);
            e.Property(x => x.LicensePlate).HasMaxLength(30);
            e.Property(x => x.Status).HasMaxLength(30);
            e.Property(x => x.FuelType).HasMaxLength(50);
            e.Property(x => x.Transmission).HasMaxLength(50);
            e.Property(x => x.PricePerDay).HasPrecision(18, 2);
        });

        modelBuilder.Entity<VehicleImage>(e =>
        {
            e.HasOne(x => x.Vehicle).WithMany(v => v.Images).HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Url).HasMaxLength(500);
            e.HasIndex(x => new { x.VehicleId, x.SortOrder });
        });

        modelBuilder.Entity<Driver>(e =>
        {
            e.HasIndex(x => x.LicenseNumber).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.LicenseNumber).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.Rating).HasPrecision(3, 2);
            e.Property(x => x.PricePerDay).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.HasIndex(x => new { x.VehicleId, x.StartDate, x.EndDate });
            e.Property(x => x.Status).HasMaxLength(40);
            e.Property(x => x.RentalType).HasMaxLength(30);
            e.Property(x => x.DriverAssignmentStatus).HasMaxLength(40);
            e.Property(x => x.VehicleRentalPrice).HasPrecision(18, 2);
            e.Property(x => x.DriverRentalPrice).HasPrecision(18, 2);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.ServiceFee).HasPrecision(18, 2);
            e.Property(x => x.InsuranceFee).HasPrecision(18, 2);
            e.Property(x => x.TotalPrice).HasPrecision(18, 2);
            e.Property(x => x.PickupLocation).HasMaxLength(255);
            e.HasOne(x => x.DriverLicense).WithMany().HasForeignKey(x => x.DriverLicenseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.IdentityVerification).WithMany().HasForeignKey(x => x.IdentityVerificationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BookingDriver>(e =>
        {
            e.HasIndex(x => x.BookingId).IsUnique();
            e.HasIndex(x => new { x.DriverId, x.StartDate, x.EndDate });
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Booking).WithOne(x => x.BookingDriver).HasForeignKey<BookingDriver>(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Driver).WithMany(x => x.BookingDrivers).HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VehicleReservationHold>(e =>
        {
            e.HasIndex(x => new { x.VehicleId, x.StartDate, x.EndDate });
            e.HasIndex(x => x.BookingId).IsUnique();
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Booking).WithOne(x => x.ReservationHold).HasForeignKey<VehicleReservationHold>(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasIndex(x => x.PayOsOrderCode).IsUnique();
            e.Property(x => x.Status).HasMaxLength(30);
            e.Property(x => x.Provider).HasMaxLength(50);
            e.Property(x => x.PayOsPaymentLinkId).HasMaxLength(255);
            e.Property(x => x.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ChatRoom>(e =>
        {
            e.HasIndex(x => x.BookingId).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Admin).WithMany().HasForeignKey(x => x.AdminId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.HasIndex(x => x.BookingId).IsUnique();
            e.HasOne(x => x.Booking).WithOne(b => b.Review!).HasForeignKey<Review>(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Vehicle).WithMany(v => v.Reviews).HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Comment).HasMaxLength(1000);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Type).HasMaxLength(30);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Promotion>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(150);
            e.Property(x => x.Subtitle).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Badge).HasMaxLength(50);
            e.Property(x => x.BadgeColor).HasMaxLength(30);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.PromoCode).HasMaxLength(50);
            e.HasIndex(x => new { x.IsActive, x.StartDate, x.EndDate });
            e.HasIndex(x => x.SortOrder);
        });
    }
}
