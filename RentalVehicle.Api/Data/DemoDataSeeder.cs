using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Vehicles.AnyAsync())
        {
            if (!await db.Users.AnyAsync(x => x.Email == "user@gmail.com"))
            {
                db.Users.Add(new User
                {
                    FullName = "Nguyen Van An",
                    Email = "user@gmail.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123456"),
                    PhoneNumber = "0909000001",
                    Address = "Ha Noi",
                    Role = Roles.Customer,
                    VerificationStatus = VerificationStatuses.Verified,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await db.Users.AnyAsync(x => x.Email == "customer2@gmail.com"))
            {
                db.Users.Add(new User
                {
                    FullName = "Tran Thi Binh",
                    Email = "customer2@gmail.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123456"),
                    PhoneNumber = "0909000002",
                    Address = "Ho Chi Minh",
                    Role = Roles.Customer,
                    VerificationStatus = VerificationStatuses.Verified,
                    CreatedAt = DateTime.UtcNow
                });
            }

            db.Vehicles.AddRange(
                new Vehicle { Name="Toyota Vios G 2024", Type="Car", Brand="Toyota", Model="Vios G", LicensePlate="30A-12345", Description="Sedan tiết kiệm nhiên liệu, phù hợp đi phố.", PricePerDay=700000, Seats=5, FuelType="Gasoline", Transmission="Automatic", ImageUrl="https://images.unsplash.com/photo-1549317661-bd32c8ce0db2", Status="Available", CreatedAt=DateTime.UtcNow },
                new Vehicle { Name="Honda CR-V L 2024", Type="Car", Brand="Honda", Model="CR-V L", LicensePlate="30B-23456", Description="SUV gia đình rộng rãi.", PricePerDay=1200000, Seats=7, FuelType="Gasoline", Transmission="Automatic", ImageUrl="https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6", Status="Available", CreatedAt=DateTime.UtcNow },
                new Vehicle { Name="VinFast VF8 Plus", Type="Car", Brand="VinFast", Model="VF8 Plus", LicensePlate="51H-34567", Description="Xe điện cao cấp.", PricePerDay=1500000, Seats=5, FuelType="Electric", Transmission="Automatic", ImageUrl="https://images.unsplash.com/photo-1593941707882-a5bba14938c7", Status="Available", CreatedAt=DateTime.UtcNow },
                new Vehicle { Name="Honda Air Blade 160", Type="Motorbike", Brand="Honda", Model="Air Blade", LicensePlate="29X-45678", Description="Xe máy tiện lợi trong thành phố.", PricePerDay=150000, Seats=2, FuelType="Gasoline", Transmission="Automatic", ImageUrl="https://images.unsplash.com/photo-1558981806-ec527fa84c39", Status="Available", CreatedAt=DateTime.UtcNow }
            );
        }

        if (!await db.Promotions.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.Promotions.AddRange(
                new Promotion
                {
                    Title = "Tự do cầm lái",
                    Subtitle = "Bắt mood phiêu du",
                    Description = "Ưu đãi dành cho hành trình tự lái trong thời gian chương trình.",
                    Badge = "GIẢM 8%",
                    BadgeColor = "Emerald",
                    ImageUrl = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=1200&q=85",
                    PromoCode = "TULAI8",
                    StartDate = now.AddDays(-30),
                    EndDate = now.AddYears(1),
                    IsActive = true,
                    SortOrder = 1,
                    CreatedAt = now
                },
                new Promotion
                {
                    Title = "Di chuyển thuận tiện",
                    Subtitle = "Linh hoạt theo giờ",
                    Description = "Chương trình ưu đãi cho khách hàng đặt xe sớm.",
                    Badge = "ƯU ĐÃI 10%",
                    BadgeColor = "Blue",
                    ImageUrl = "https://images.unsplash.com/photo-1493238792000-8113da705763?w=1200&q=85",
                    PromoCode = "SOM10",
                    StartDate = now.AddDays(-30),
                    EndDate = now.AddYears(1),
                    IsActive = true,
                    SortOrder = 2,
                    CreatedAt = now
                },
                new Promotion
                {
                    Title = "Không gian rộng rãi",
                    Subtitle = "Hành trình thoải mái",
                    Description = "Ưu đãi cho nhóm khách thuê xe gia đình.",
                    Badge = "GIẢM 120K",
                    BadgeColor = "Amber",
                    ImageUrl = "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=1200&q=85",
                    PromoCode = "GIADINH120",
                    StartDate = now.AddDays(-30),
                    EndDate = now.AddYears(1),
                    IsActive = true,
                    SortOrder = 3,
                    CreatedAt = now
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
