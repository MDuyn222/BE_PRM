using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.DTOs.Promotions;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Services;

public interface IPromotionService
{
    Task<List<PromotionDto>> ListActiveAsync(CancellationToken ct = default);
    Task<List<PromotionDto>> ListAdminAsync(CancellationToken ct = default);
    Task<PromotionDto?> GetAsync(int id, CancellationToken ct = default);
    Task<(bool ok, string? error, PromotionDto? data)> CreateAsync(SavePromotionRequest request, CancellationToken ct = default);
    Task<(bool ok, string? error, PromotionDto? data)> UpdateAsync(int id, SavePromotionRequest request, CancellationToken ct = default);
    Task<(bool ok, string? error)> DeleteAsync(int id, CancellationToken ct = default);
}

public class PromotionService(AppDbContext db) : IPromotionService
{
    private static readonly HashSet<string> AllowedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "Emerald", "Blue", "Amber", "Rose", "Violet", "Zinc"
    };

    public async Task<List<PromotionDto>> ListActiveAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var items = await db.Promotions.AsNoTracking()
            .Where(x => x.IsActive && x.StartDate <= now && x.EndDate >= now)
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return items.Select(x => ToDto(x, now)).ToList();
    }

    public async Task<List<PromotionDto>> ListAdminAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var items = await db.Promotions.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return items.Select(x => ToDto(x, now)).ToList();
    }

    public async Task<PromotionDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var item = await db.Promotions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? null : ToDto(item, DateTime.UtcNow);
    }

    public async Task<(bool ok, string? error, PromotionDto? data)> CreateAsync(SavePromotionRequest request, CancellationToken ct = default)
    {
        var error = Validate(request);
        if (error is not null) return (false, error, null);

        var now = DateTime.UtcNow;
        var item = new Promotion { CreatedAt = now };
        Apply(item, request, now);
        db.Promotions.Add(item);
        await db.SaveChangesAsync(ct);
        return (true, null, ToDto(item, now));
    }

    public async Task<(bool ok, string? error, PromotionDto? data)> UpdateAsync(int id, SavePromotionRequest request, CancellationToken ct = default)
    {
        var item = await db.Promotions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return (false, "Không tìm thấy ưu đãi.", null);

        var error = Validate(request);
        if (error is not null) return (false, error, null);

        var now = DateTime.UtcNow;
        Apply(item, request, now);
        item.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return (true, null, ToDto(item, now));
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await db.Promotions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return (false, "Không tìm thấy ưu đãi.");
        db.Promotions.Remove(item);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    private static string? Validate(SavePromotionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return "Tiêu đề ưu đãi là bắt buộc.";
        if (request.Title.Trim().Length > 150) return "Tiêu đề không được vượt quá 150 ký tự.";
        if (request.Subtitle?.Trim().Length > 200) return "Mô tả ngắn không được vượt quá 200 ký tự.";
        if (request.Description?.Trim().Length > 1000) return "Mô tả chi tiết không được vượt quá 1000 ký tự.";
        if (string.IsNullOrWhiteSpace(request.Badge)) return "Nhãn ưu đãi là bắt buộc.";
        if (request.Badge.Trim().Length > 50) return "Nhãn ưu đãi không được vượt quá 50 ký tự.";
        if (!AllowedColors.Contains(request.BadgeColor)) return "Màu nhãn ưu đãi không hợp lệ.";
        if (string.IsNullOrWhiteSpace(request.ImageUrl)) return "Ảnh ưu đãi là bắt buộc.";
        if (request.ImageUrl.Trim().Length > 500) return "Đường dẫn ảnh không được vượt quá 500 ký tự.";
        if (!Uri.TryCreate(request.ImageUrl.Trim(), UriKind.RelativeOrAbsolute, out _)) return "Đường dẫn ảnh ưu đãi không hợp lệ.";
        if (request.PromoCode?.Trim().Length > 50) return "Mã ưu đãi không được vượt quá 50 ký tự.";
        if (request.StartDate == default || request.EndDate == default) return "Ngày bắt đầu và kết thúc là bắt buộc.";
        if (request.EndDate <= request.StartDate) return "Ngày kết thúc phải sau ngày bắt đầu.";
        if (request.SortOrder < 0) return "Thứ tự hiển thị không được âm.";
        return null;
    }

    private static void Apply(Promotion item, SavePromotionRequest request, DateTime now)
    {
        item.Title = request.Title.Trim();
        item.Subtitle = Clean(request.Subtitle);
        item.Description = Clean(request.Description);
        item.Badge = request.Badge.Trim();
        item.BadgeColor = NormalizeColor(request.BadgeColor);
        item.ImageUrl = request.ImageUrl.Trim();
        item.PromoCode = Clean(request.PromoCode)?.ToUpperInvariant();
        item.StartDate = EnsureUtc(request.StartDate);
        item.EndDate = EnsureUtc(request.EndDate);
        item.IsActive = request.IsActive;
        item.SortOrder = request.SortOrder;
        if (item.CreatedAt == default) item.CreatedAt = now;
    }

    private static string NormalizeColor(string value) =>
        AllowedColors.First(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static PromotionDto ToDto(Promotion x, DateTime now) => new()
    {
        Id = x.Id,
        Title = x.Title,
        Subtitle = x.Subtitle,
        Description = x.Description,
        Badge = x.Badge,
        BadgeColor = x.BadgeColor,
        ImageUrl = x.ImageUrl,
        PromoCode = x.PromoCode,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        IsActive = x.IsActive,
        IsCurrentlyActive = x.IsActive && x.StartDate <= now && x.EndDate >= now,
        SortOrder = x.SortOrder,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
