using Microsoft.EntityFrameworkCore;
using RentalVehicle.Api.Data;
using RentalVehicle.Api.DTOs.Verification;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Services;

public interface IVerificationService
{
    Task<VerificationProfileDto?> GetProfileAsync(int userId, CancellationToken ct = default);
    Task<(bool ok, string? error, object? data)> SaveDriverLicenseAsync(int userId, SaveDriverLicenseRequest request, CancellationToken ct = default);
    Task<(bool ok, string? error, object? data)> SaveIdentityAsync(int userId, SaveIdentityVerificationRequest request, CancellationToken ct = default);
    Task<List<object>> ListPendingAsync(CancellationToken ct = default);
    Task<(bool ok, string? error)> VerifyDriverLicenseAsync(int id, VerifyRequest request, CancellationToken ct = default);
    Task<(bool ok, string? error)> VerifyIdentityAsync(int id, VerifyRequest request, CancellationToken ct = default);
}

public class VerificationService(AppDbContext db) : IVerificationService
{
    public async Task<VerificationProfileDto?> GetProfileAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking()
            .Include(x => x.DriverLicense).Include(x => x.IdentityVerification)
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return null;
        return new VerificationProfileDto
        {
            UserId = user.Id,
            UserVerificationStatus = user.VerificationStatus,
            DriverLicense = user.DriverLicense is null ? null : LicenseDto(user.DriverLicense),
            IdentityVerification = user.IdentityVerification is null ? null : IdentityDto(user.IdentityVerification)
        };
    }

    public async Task<(bool ok, string? error, object? data)> SaveDriverLicenseAsync(int userId, SaveDriverLicenseRequest r, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(r.LicenseNumber) || string.IsNullOrWhiteSpace(r.LicenseType))
            return (false, "Số và hạng giấy phép lái xe là bắt buộc.", null);
        if (string.IsNullOrWhiteSpace(r.FrontImage) || string.IsNullOrWhiteSpace(r.BackImage))
            return (false, "Phải tải đủ ảnh mặt trước và mặt sau GPLX.", null);
        if (r.ExpireDate <= r.IssueDate) return (false, "Ngày hết hạn GPLX không hợp lệ.", null);
        if (await db.DriverLicenses.AnyAsync(x => x.UserId != userId && x.LicenseNumber == r.LicenseNumber.Trim(), ct))
            return (false, "Số GPLX đã được sử dụng.", null);

        var entity = await db.DriverLicenses.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (entity is null)
        {
            entity = new DriverLicense { UserId = userId, CreatedAt = DateTime.UtcNow };
            db.DriverLicenses.Add(entity);
        }
        entity.LicenseNumber = r.LicenseNumber.Trim();
        entity.LicenseType = r.LicenseType.Trim();
        entity.IssueDate = r.IssueDate;
        entity.ExpireDate = r.ExpireDate;
        entity.FrontImage = r.FrontImage.Trim();
        entity.BackImage = r.BackImage.Trim();
        entity.VerificationStatus = VerificationStatuses.Pending;
        entity.RejectionReason = null;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null, LicenseDto(entity));
    }

    public async Task<(bool ok, string? error, object? data)> SaveIdentityAsync(int userId, SaveIdentityVerificationRequest r, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(r.CCCDNumber)) return (false, "Số CCCD là bắt buộc.", null);
        if (string.IsNullOrWhiteSpace(r.FrontImage) || string.IsNullOrWhiteSpace(r.BackImage))
            return (false, "Phải tải đủ ảnh mặt trước và mặt sau CCCD.", null);
        if (await db.IdentityVerifications.AnyAsync(x => x.UserId != userId && x.CCCDNumber == r.CCCDNumber.Trim(), ct))
            return (false, "Số CCCD đã được sử dụng.", null);

        var entity = await db.IdentityVerifications.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (entity is null)
        {
            entity = new IdentityVerification { UserId = userId, CreatedAt = DateTime.UtcNow };
            db.IdentityVerifications.Add(entity);
        }
        entity.CCCDNumber = r.CCCDNumber.Trim();
        entity.FrontImage = r.FrontImage.Trim();
        entity.BackImage = r.BackImage.Trim();
        entity.Status = VerificationStatuses.Pending;
        entity.RejectionReason = null;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null, IdentityDto(entity));
    }

    public async Task<List<object>> ListPendingAsync(CancellationToken ct = default)
    {
        var licenses = await db.DriverLicenses.AsNoTracking().Include(x => x.User)
            .Where(x => x.VerificationStatus == VerificationStatuses.Pending).ToListAsync(ct);
        var identities = await db.IdentityVerifications.AsNoTracking().Include(x => x.User)
            .Where(x => x.Status == VerificationStatuses.Pending).ToListAsync(ct);
        var result = new List<object>();
        result.AddRange(licenses.Select(x => (object)new { type = "DriverLicense", id = x.Id, userId = x.UserId, userName = x.User.FullName, number = x.LicenseNumber, frontImage = x.FrontImage, backImage = x.BackImage, status = x.VerificationStatus, expireDate = (DateTime?)x.ExpireDate }));
        result.AddRange(identities.Select(x => (object)new { type = "IdentityVerification", id = x.Id, userId = x.UserId, userName = x.User.FullName, number = x.CCCDNumber, frontImage = x.FrontImage, backImage = x.BackImage, status = x.Status, expireDate = (DateTime?)null }));
        return result;
    }

    public async Task<(bool ok, string? error)> VerifyDriverLicenseAsync(int id, VerifyRequest r, CancellationToken ct = default)
    {
        if (r.Status is not (VerificationStatuses.Verified or VerificationStatuses.Rejected)) return (false, "Trạng thái xác minh không hợp lệ.");
        var entity = await db.DriverLicenses.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return (false, "Không tìm thấy GPLX.");
        entity.VerificationStatus = entity.ExpireDate <= DateTime.UtcNow ? VerificationStatuses.Expired : r.Status;
        entity.RejectionReason = r.Status == VerificationStatuses.Rejected ? r.Reason : null;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.User.VerificationStatus = entity.VerificationStatus == VerificationStatuses.Verified ? VerificationStatuses.Verified : entity.User.VerificationStatus;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> VerifyIdentityAsync(int id, VerifyRequest r, CancellationToken ct = default)
    {
        if (r.Status is not (VerificationStatuses.Verified or VerificationStatuses.Rejected)) return (false, "Trạng thái xác minh không hợp lệ.");
        var entity = await db.IdentityVerifications.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return (false, "Không tìm thấy CCCD.");
        entity.Status = r.Status; entity.RejectionReason = r.Status == VerificationStatuses.Rejected ? r.Reason : null; entity.UpdatedAt = DateTime.UtcNow;
        entity.User.IdentityNumber = entity.CCCDNumber;
        entity.User.VerificationStatus = r.Status == VerificationStatuses.Verified ? VerificationStatuses.Verified : entity.User.VerificationStatus;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    private static object LicenseDto(DriverLicense x) => new { x.Id, x.LicenseNumber, x.LicenseType, x.IssueDate, x.ExpireDate, x.FrontImage, x.BackImage, status = x.VerificationStatus, x.RejectionReason };
    private static object IdentityDto(IdentityVerification x) => new { x.Id, x.CCCDNumber, x.FrontImage, x.BackImage, status = x.Status, x.RejectionReason };
}
