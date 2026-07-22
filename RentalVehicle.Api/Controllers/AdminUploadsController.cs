using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.DTOs.Vehicles;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.AdminRoles)]
[Route("api/admin/uploads")]
public class AdminUploadsController(IWebHostEnvironment env) : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]]
    };

    [HttpPost("vehicle-image")]
    [RequestSizeLimit(MaxBytes)]
    public Task<IActionResult> UploadVehicleImage(IFormFile? file, CancellationToken ct) =>
        UploadImage(file, "vehicles", ct);

    [HttpPost("promotion-image")]
    [RequestSizeLimit(MaxBytes)]
    public Task<IActionResult> UploadPromotionImage(IFormFile? file, CancellationToken ct) =>
        UploadImage(file, "promotions", ct);

    private async Task<IActionResult> UploadImage(IFormFile? file, string category, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "File là bắt buộc." });
        if (file.Length > MaxBytes) return BadRequest(new { message = "File vượt quá 5MB." });

        var ext = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();
        if (!Signatures.ContainsKey(ext)) return BadRequest(new { message = "Chỉ hỗ trợ JPG, PNG, WEBP." });

        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var read = await input.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (read < 4 || !Signatures[ext].Any(signature => header.Take(signature.Length).SequenceEqual(signature)))
            return BadRequest(new { message = "Nội dung file không khớp định dạng ảnh." });
        input.Position = 0;

        var root = string.IsNullOrWhiteSpace(env.WebRootPath) ? Path.Combine(env.ContentRootPath, "wwwroot") : env.WebRootPath;
        var folder = Path.Combine(root, "uploads", category);
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.GetFullPath(Path.Combine(folder, fileName));
        var safeRoot = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(safeRoot, StringComparison.Ordinal)) return BadRequest(new { message = "Đường dẫn file không hợp lệ." });

        await using var output = System.IO.File.Create(fullPath);
        await input.CopyToAsync(output, ct);
        return Ok(new UploadResponse { Url = $"/uploads/{category}/{fileName}" });
    }
}
