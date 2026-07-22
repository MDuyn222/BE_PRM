using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Services;

public interface IDashboardService { Task<object> GetAsync(CancellationToken ct = default); }
public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<object> GetAsync(CancellationToken ct = default)
    {
        var totalVehicles = await db.Vehicles.CountAsync(ct);
        var availableVehicles = await db.Vehicles.CountAsync(x => x.Status == "Available", ct);
        var users = await db.Users.CountAsync(x => x.Role == Roles.Customer, ct);
        var bookingCount = await db.Bookings.CountAsync(ct);
        var now = DateTime.UtcNow;
        var activePromotions = await db.Promotions.CountAsync(x => x.IsActive && x.StartDate <= now && x.EndDate >= now, ct);
        var waitingApproval = await db.Bookings.CountAsync(x => x.Status == BookingStatuses.WaitingApproval || x.Status == BookingStatuses.WaitingDriverAssignment, ct);
        var revenue = await db.Payments.Where(x => x.Status == "Paid").SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var recent = await db.Bookings.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(5)
            .Select(x => new { x.Id, customer = x.User.FullName, vehicle = x.Vehicle.Name, x.TotalPrice, x.Status, x.CreatedAt }).ToListAsync(ct);
        var popular = await db.Vehicles.AsNoTracking().OrderByDescending(x => x.Bookings.Count(b => b.Status != BookingStatuses.Cancelled && b.Status != BookingStatuses.Rejected))
            .Take(5).Select(x => new { x.Id, x.Name, bookingCount = x.Bookings.Count }).ToListAsync(ct);
        return new { totalVehicles, availableVehicles, users, bookingCount, waitingApproval, activePromotions, revenue, recentBookings = recent, popularVehicles = popular };
    }
}
