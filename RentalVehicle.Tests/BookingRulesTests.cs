using Xunit;
using RentalVehicle.Api.Services;

namespace RentalVehicle.Tests;

public class BookingRulesTests
{
    [Fact]
    public void CalculatePrice_UsesInclusiveRentalDaysAndBackendFees()
    {
        var result = BookingRules.CalculatePrice(
            new DateTime(2026, 7, 21),
            new DateTime(2026, 7, 23),
            vehiclePricePerDay: 500_000m,
            driverPricePerDay: 300_000m);

        Assert.Equal(3, result.RentalDays);
        Assert.Equal(1_500_000m, result.VehiclePrice);
        Assert.Equal(900_000m, result.DriverPrice);
        Assert.Equal(150_000m, result.ServiceFee);
        Assert.Equal(180_000m, result.InsuranceFee);
        Assert.Equal(2_730_000m, result.TotalPrice);
    }

    [Theory]
    [InlineData("2026-07-21", "2026-07-23", "2026-07-23", "2026-07-24", true)]
    [InlineData("2026-07-21", "2026-07-22", "2026-07-23", "2026-07-24", false)]
    public void Overlaps_UsesInclusiveBoundary(string aStart, string aEnd, string bStart, string bEnd, bool expected)
    {
        Assert.Equal(expected, BookingRules.Overlaps(DateTime.Parse(aStart), DateTime.Parse(aEnd), DateTime.Parse(bStart), DateTime.Parse(bEnd)));
    }

    [Theory]
    [InlineData(1_000_000, 1_000_000, true)]
    [InlineData(1_000_000, 1_100_000, true)]
    [InlineData(1_000_000, 999_999, false)]
    public void IsPaidInFull_RequiresTheEntireBackendTotal(decimal total, decimal paid, bool expected)
    {
        Assert.Equal(expected, BookingRules.IsPaidInFull(total, paid));
    }

    [Fact]
    public void CalculatePrice_RejectsInvalidDateRange()
    {
        Assert.Throws<ArgumentException>(() => BookingRules.CalculatePrice(new DateTime(2026, 7, 22), new DateTime(2026, 7, 21), 100_000m));
    }
}
