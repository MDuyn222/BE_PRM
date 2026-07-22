namespace RentalVehicle.Api.Models;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Customer = "Customer";
    public const string AdminRoles = SuperAdmin + "," + Manager + "," + Staff;
}

public static class RentalTypes
{
    public const string SelfDrive = "SelfDrive";
    public const string DriverIncluded = "DriverIncluded";
    public static readonly string[] All = [SelfDrive, DriverIncluded];
}

public static class BookingStatuses
{
    public const string PendingPayment = "PendingPayment";
    public const string Paid = "Paid";
    public const string WaitingApproval = "WaitingApproval";
    public const string WaitingDriverAssignment = "WaitingDriverAssignment";
    public const string Approved = "Approved";
    public const string Pickup = "Pickup";
    public const string Renting = "Renting";
    public const string Returning = "Returning";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Rejected = "Rejected";
    public const string Expired = "Expired";

    public static readonly string[] VehicleBlocking =
    [
        PendingPayment, Paid, WaitingApproval, WaitingDriverAssignment,
        Approved, Pickup, Renting, Returning
    ];

    public static readonly string[] DriverBlocking =
    [
        Approved, Pickup, Renting, Returning
    ];
}

public static class VerificationStatuses
{
    public const string Pending = "Pending";
    public const string Verified = "Verified";
    public const string Rejected = "Rejected";
    public const string Expired = "Expired";
}

public static class DriverStatuses
{
    public const string Available = "Available";
    public const string Busy = "Busy";
    public const string Inactive = "Inactive";
}

public static class HoldStatuses
{
    public const string Active = "Active";
    public const string Converted = "Converted";
    public const string Released = "Released";
    public const string Expired = "Expired";
}
