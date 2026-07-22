namespace RentalVehicle.Api.Services;

public readonly record struct BookingPriceBreakdown(
    int RentalDays,
    decimal VehiclePrice,
    decimal DriverPrice,
    decimal ServiceFee,
    decimal InsuranceFee,
    decimal TotalPrice);

public static class BookingRules
{
    public static bool Overlaps(DateTime firstStart, DateTime firstEnd, DateTime secondStart, DateTime secondEnd) =>
        firstStart <= secondEnd && firstEnd >= secondStart;

    public static bool IsPaidInFull(decimal totalPrice, decimal paidAmount) =>
        totalPrice >= 0m && paidAmount >= totalPrice;

    public static BookingPriceBreakdown CalculatePrice(DateTime startDate, DateTime endDate, decimal vehiclePricePerDay, decimal driverPricePerDay = 0m)
    {
        if (endDate.Date < startDate.Date) throw new ArgumentException("Ngày kết thúc phải từ ngày bắt đầu trở đi.");
        if (vehiclePricePerDay < 0 || driverPricePerDay < 0) throw new ArgumentOutOfRangeException(nameof(vehiclePricePerDay), "Đơn giá không được âm.");

        var rentalDays = (endDate.Date - startDate.Date).Days + 1;
        var vehiclePrice = rentalDays * vehiclePricePerDay;
        var driverPrice = rentalDays * driverPricePerDay;
        var serviceFee = Math.Round(vehiclePrice * 0.10m, 0, MidpointRounding.AwayFromZero);
        var insuranceFee = rentalDays * 60_000m;
        return new BookingPriceBreakdown(rentalDays, vehiclePrice, driverPrice, serviceFee, insuranceFee, vehiclePrice + driverPrice + serviceFee + insuranceFee);
    }
}
