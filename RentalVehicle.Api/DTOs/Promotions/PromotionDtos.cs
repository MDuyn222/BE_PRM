namespace RentalVehicle.Api.DTOs.Promotions;

public class PromotionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string Badge { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "Emerald";
    public string ImageUrl { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentlyActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SavePromotionRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string Badge { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "Emerald";
    public string ImageUrl { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
