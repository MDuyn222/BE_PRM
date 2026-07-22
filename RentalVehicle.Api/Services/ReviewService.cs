using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.DTOs.Reviews;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Services;

public interface IReviewService
{
    Task<List<ReviewDto>> ListByVehicleAsync(int vehicleId, CancellationToken ct = default);
    Task<(bool success, string? error, ReviewDto? data)> CreateAsync(int userId, CreateReviewRequest request, CancellationToken ct = default);
}

public class ReviewService(AppDbContext db) : IReviewService
{
    public Task<List<ReviewDto>> ListByVehicleAsync(int vehicleId, CancellationToken ct = default) =>
        db.Reviews
            .AsNoTracking()
            .Where(r => r.VehicleId == vehicleId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                BookingId = r.BookingId,
                VehicleId = r.VehicleId,
                UserId = r.UserId,
                UserFullName = r.User.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync(ct);

    public async Task<(bool success, string? error, ReviewDto? data)> CreateAsync(int userId, CreateReviewRequest request, CancellationToken ct = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return (false, "Rating must be between 1 and 5.", null);
        if (request.Comment is { Length: > 1000 })
            return (false, "Comment must be at most 1000 characters.", null);

        var booking = await db.Bookings
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, ct);
        if (booking is null) return (false, "Không tìm thấy đơn thuê.", null);
        if (booking.UserId != userId) return (false, "Bạn chỉ có thể đánh giá đơn thuê của mình.", null);
        if (booking.Status != "Completed") return (false, "Chỉ đơn thuê đã hoàn tất mới có thể được đánh giá.", null);
        if (booking.Review is not null) return (false, "Đơn thuê này đã được đánh giá.", null);

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return (false, "Không tìm thấy người dùng.", null);

        var review = new Review
        {
            BookingId = booking.Id,
            VehicleId = booking.VehicleId,
            UserId = userId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);

        return (true, null, new ReviewDto
        {
            Id = review.Id,
            BookingId = review.BookingId,
            VehicleId = review.VehicleId,
            UserId = review.UserId,
            UserFullName = user.FullName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
        });
    }
}
