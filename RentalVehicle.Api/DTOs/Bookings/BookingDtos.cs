namespace RentalVehicle.Api.DTOs.Bookings;

public class CreateBookingRequest
{
    public int VehicleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string RentalType { get; set; } = string.Empty;
    public int? DriverId { get; set; }
    public bool AssignDriverLater { get; set; }
    public string? PickupLocation { get; set; }
    public string? Note { get; set; }
}

public class BookingDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string? VehicleImageUrl { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string RentalType { get; set; } = string.Empty;
    public int RentalDays { get; set; }
    public decimal VehicleRentalPrice { get; set; }
    public decimal DriverRentalPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal InsuranceFee { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DriverAssignmentStatus { get; set; } = string.Empty;
    public string? PickupLocation { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? HoldExpiresAt { get; set; }
    public DriverSummaryDto? Driver { get; set; }
    public VerificationSummaryDto? DriverLicense { get; set; }
    public VerificationSummaryDto? IdentityVerification { get; set; }
    public PaymentSummaryDto? Payment { get; set; }
}

public class DriverSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public int ExperienceYears { get; set; }
    public decimal Rating { get; set; }
    public decimal PricePerDay { get; set; }
}

public class VerificationSummaryDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string FrontImage { get; set; } = string.Empty;
    public string BackImage { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpireDate { get; set; }
}

public class PaymentSummaryDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? CheckoutUrl { get; set; }
}

public class AvailabilityDto
{
    public int VehicleId { get; set; }
    public bool Available { get; set; }
    public string? Message { get; set; }
    public List<BlockedRangeDto> BlockedRanges { get; set; } = new();
}

public class BlockedRangeDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class AdminBookingQuery
{
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedBookingsDto
{
    public List<BookingDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AssignDriverRequest { public int DriverId { get; set; } }
public class RejectBookingRequest { public string? Reason { get; set; } }
