using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalVehicle.Api.Models;

namespace RentalVehicle.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Customer)]
[Route("api/uploads")]
public class UploadsController(IWebHostEnvironment env) : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]], [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47]], [".webp"] = [[0x52, 0x49, 0x46, 0x46]]
    };

    [HttpPost("document")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> Document(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "File là bắt buộc." });
        if (file.Length > MaxBytes) return BadRequest(new { message = "File vượt quá 5MB." });
        var ext = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();
        if (!Signatures.ContainsKey(ext)) return BadRequest(new { message = "Chỉ hỗ trợ JPG, PNG, WEBP." });
        await using var input = file.OpenReadStream();
        var header = new byte[12]; var read = await input.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (read < 4 || !Signatures[ext].Any(sig => header.Take(sig.Length).SequenceEqual(sig))) return BadRequest(new { message = "Nội dung file không khớp định dạng ảnh." });
        input.Position = 0;
        var root = string.IsNullOrWhiteSpace(env.WebRootPath) ? Path.Combine(env.ContentRootPath, "wwwroot") : env.WebRootPath;
        var folder = Path.Combine(root, "uploads", "documents"); Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{ext}"; var full = Path.GetFullPath(Path.Combine(folder, fileName));
        if (!full.StartsWith(Path.GetFullPath(folder), StringComparison.Ordinal)) return BadRequest(new { message = "Đường dẫn file không hợp lệ." });
        await using var output = System.IO.File.Create(full); await input.CopyToAsync(output, ct);
        return Ok(new { url = $"/uploads/documents/{fileName}" });
    }
}
