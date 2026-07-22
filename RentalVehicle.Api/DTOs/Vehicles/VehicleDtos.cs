namespace RentalVehicle.Api.DTOs.Vehicles;

public class VehicleListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal PricePerDay { get; set; }
    public string? ImageUrl { get; set; }
    public int? Seats { get; set; }
    public string Status { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class VehicleDetailDto : VehicleListItemDto
{
    public string? Model { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<VehicleImageDto> Images { get; set; } = new();
}

public class VehicleImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class VehicleQuery
{
    public string? Type { get; set; }
    public string? Status { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Keyword { get; set; }
}

public class CreateVehicleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PricePerDay { get; set; }
    public string? ImageUrl { get; set; }
    public int? Seats { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string Status { get; set; } = "Available";
    /// <summary>Optional extra image URLs to attach when creating</summary>
    public List<string>? Images { get; set; }
}

public class UpdateVehicleRequest : CreateVehicleRequest;

public class UploadResponse
{
    public string Url { get; set; } = string.Empty;
}

public class AddVehicleImageRequest
{
    public string Url { get; set; } = string.Empty;
}

public class ReorderImagesRequest
{
    public List<int> ImageIds { get; set; } = new();
}
