using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.DTOs.Vehicles;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Services;

public interface IVehicleService
{
    Task<List<VehicleListItemDto>> ListAsync(VehicleQuery query, bool availableOnly, CancellationToken ct = default);
    Task<VehicleDetailDto?> GetByIdAsync(int id, bool availableOnly, CancellationToken ct = default);
    Task<(bool success, string? error, VehicleDetailDto? data)> CreateAsync(CreateVehicleRequest request, CancellationToken ct = default);
    Task<(bool success, string? error, VehicleDetailDto? data)> UpdateAsync(int id, UpdateVehicleRequest request, CancellationToken ct = default);
    Task<(bool success, string? error)> DeleteAsync(int id, CancellationToken ct = default);

    Task<(bool success, string? error, VehicleImageDto? data)> AddImageAsync(int vehicleId, string url, CancellationToken ct = default);
    Task<(bool success, string? error)> RemoveImageAsync(int vehicleId, int imageId, CancellationToken ct = default);
    Task<(bool success, string? error)> ReorderImagesAsync(int vehicleId, List<int> imageIds, CancellationToken ct = default);
}

public class VehicleService(AppDbContext db) : IVehicleService
{
    private static readonly string[] AllowedTypes = ["Car", "Motorbike"];
    private static readonly string[] AllowedStatuses = ["Available", "Reserved", "Rented", "Maintenance", "Inactive"];

    public async Task<List<VehicleListItemDto>> ListAsync(VehicleQuery query, bool availableOnly, CancellationToken ct = default)
    {
        var q = db.Vehicles.AsNoTracking().AsQueryable();

        if (availableOnly)
            q = q.Where(v => v.Status == "Available");
        else if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(v => v.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.Type))
            q = q.Where(v => v.Type == query.Type);
        if (query.MinPrice.HasValue)
            q = q.Where(v => v.PricePerDay >= query.MinPrice.Value);
        if (query.MaxPrice.HasValue)
            q = q.Where(v => v.PricePerDay <= query.MaxPrice.Value);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToLower();
            q = q.Where(v => v.Name.ToLower().Contains(kw) || v.Brand.ToLower().Contains(kw));
        }

        return await q
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VehicleListItemDto
            {
                Id = v.Id,
                Name = v.Name,
                Type = v.Type,
                Brand = v.Brand,
                PricePerDay = v.PricePerDay,
                ImageUrl = v.ImageUrl,
                Seats = v.Seats,
                Status = v.Status,
                ReviewCount = v.Reviews.Count,
                AverageRating = v.Reviews.Any() ? v.Reviews.Average(r => (double)r.Rating) : 0,
            })
            .ToListAsync(ct);
    }

    public async Task<VehicleDetailDto?> GetByIdAsync(int id, bool availableOnly, CancellationToken ct = default)
    {
        var v = await db.Vehicles
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Reviews)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return null;
        if (availableOnly && v.Status != "Available") return null;
        return ToDetail(v);
    }

    public async Task<(bool success, string? error, VehicleDetailDto? data)> CreateAsync(CreateVehicleRequest request, CancellationToken ct = default)
    {
        var error = Validate(request);
        if (error is not null) return (false, error, null);

        var plate = request.LicensePlate.Trim();
        if (await db.Vehicles.AnyAsync(v => v.LicensePlate.ToLower() == plate.ToLower(), ct))
            return (false, "Biển số xe đã tồn tại.", null);

        var now = DateTime.UtcNow;
        var vehicle = new Vehicle
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            Brand = request.Brand.Trim(),
            Model = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim(),
            LicensePlate = plate,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            PricePerDay = request.PricePerDay,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Seats = request.Seats,
            FuelType = string.IsNullOrWhiteSpace(request.FuelType) ? null : request.FuelType.Trim(),
            Transmission = string.IsNullOrWhiteSpace(request.Transmission) ? null : request.Transmission.Trim(),
            Status = request.Status,
            CreatedAt = now,
        };

        if (request.Images is { Count: > 0 })
        {
            var order = 0;
            foreach (var url in request.Images.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct())
            {
                vehicle.Images.Add(new VehicleImage
                {
                    Url = url.Trim(),
                    SortOrder = order++,
                    CreatedAt = now,
                });
            }
        }

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(ct);
        return (true, null, ToDetail(vehicle));
    }

    public async Task<(bool success, string? error, VehicleDetailDto? data)> UpdateAsync(int id, UpdateVehicleRequest request, CancellationToken ct = default)
    {
        var vehicle = await db.Vehicles
            .Include(v => v.Images)
            .Include(v => v.Reviews)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vehicle is null) return (false, "Không tìm thấy xe.", null);

        var error = Validate(request);
        if (error is not null) return (false, error, null);

        var plate = request.LicensePlate.Trim();
        if (await db.Vehicles.AnyAsync(v => v.Id != id && v.LicensePlate.ToLower() == plate.ToLower(), ct))
            return (false, "Biển số xe đã tồn tại.", null);

        vehicle.Name = request.Name.Trim();
        vehicle.Type = request.Type;
        vehicle.Brand = request.Brand.Trim();
        vehicle.Model = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim();
        vehicle.LicensePlate = plate;
        vehicle.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        vehicle.PricePerDay = request.PricePerDay;
        vehicle.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        vehicle.Seats = request.Seats;
        vehicle.FuelType = string.IsNullOrWhiteSpace(request.FuelType) ? null : request.FuelType.Trim();
        vehicle.Transmission = string.IsNullOrWhiteSpace(request.Transmission) ? null : request.Transmission.Trim();
        vehicle.Status = request.Status;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return (true, null, ToDetail(vehicle));
    }

    public async Task<(bool success, string? error)> DeleteAsync(int id, CancellationToken ct = default)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vehicle is null) return (false, "Không tìm thấy xe.");

        var hasBookings = await db.Bookings.AnyAsync(b => b.VehicleId == id, ct);
        if (hasBookings) return (false, "Không thể xóa xe đã phát sinh đơn thuê.");

        db.Vehicles.Remove(vehicle);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool success, string? error, VehicleImageDto? data)> AddImageAsync(int vehicleId, string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return (false, "Đường dẫn ảnh là bắt buộc.", null);

        var vehicle = await db.Vehicles.Include(v => v.Images).FirstOrDefaultAsync(v => v.Id == vehicleId, ct);
        if (vehicle is null) return (false, "Không tìm thấy xe.", null);

        var nextOrder = vehicle.Images.Count == 0 ? 0 : vehicle.Images.Max(i => i.SortOrder) + 1;
        var image = new VehicleImage
        {
            VehicleId = vehicleId,
            Url = url.Trim(),
            SortOrder = nextOrder,
            CreatedAt = DateTime.UtcNow,
        };
        db.VehicleImages.Add(image);
        await db.SaveChangesAsync(ct);

        return (true, null, new VehicleImageDto { Id = image.Id, Url = image.Url, SortOrder = image.SortOrder });
    }

    public async Task<(bool success, string? error)> RemoveImageAsync(int vehicleId, int imageId, CancellationToken ct = default)
    {
        var image = await db.VehicleImages.FirstOrDefaultAsync(i => i.Id == imageId && i.VehicleId == vehicleId, ct);
        if (image is null) return (false, "Không tìm thấy ảnh.");
        db.VehicleImages.Remove(image);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool success, string? error)> ReorderImagesAsync(int vehicleId, List<int> imageIds, CancellationToken ct = default)
    {
        var images = await db.VehicleImages.Where(i => i.VehicleId == vehicleId).ToListAsync(ct);
        if (images.Count != imageIds.Count) return (false, "Danh sách ảnh không khớp dữ liệu hiện có.");
        var imagesById = images.ToDictionary(i => i.Id);
        for (var i = 0; i < imageIds.Count; i++)
        {
            if (!imagesById.TryGetValue(imageIds[i], out var img)) return (false, $"Image {imageIds[i]} not in vehicle.");
            img.SortOrder = i;
        }
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    private static string? Validate(CreateVehicleRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) return "Tên xe là bắt buộc.";
        if (!AllowedTypes.Contains(r.Type)) return "Loại xe phải là Car hoặc Motorbike.";
        if (string.IsNullOrWhiteSpace(r.Brand)) return "Hãng xe là bắt buộc.";
        if (string.IsNullOrWhiteSpace(r.LicensePlate)) return "Biển số xe là bắt buộc.";
        if (r.PricePerDay <= 0) return "Giá thuê mỗi ngày phải lớn hơn 0.";
        if (!AllowedStatuses.Contains(r.Status)) return "Trạng thái xe không hợp lệ.";
        if (r.Type == "Car" && (!r.Seats.HasValue || r.Seats.Value <= 0)) return "Ô tô bắt buộc phải có số chỗ ngồi.";
        return null;
    }

    private static VehicleDetailDto ToDetail(Vehicle v) => new()
    {
        Id = v.Id,
        Name = v.Name,
        Type = v.Type,
        Brand = v.Brand,
        PricePerDay = v.PricePerDay,
        ImageUrl = v.ImageUrl,
        Seats = v.Seats,
        Status = v.Status,
        Model = v.Model,
        LicensePlate = v.LicensePlate,
        Description = v.Description,
        FuelType = v.FuelType,
        Transmission = v.Transmission,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt,
        ReviewCount = v.Reviews.Count,
        AverageRating = v.Reviews.Count > 0 ? v.Reviews.Average(r => (double)r.Rating) : 0,
        Images = v.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new VehicleImageDto { Id = i.Id, Url = i.Url, SortOrder = i.SortOrder })
            .ToList(),
    };
}
