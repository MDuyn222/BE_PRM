using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RentalVehicle.Api.Data.Migrations;

public partial class AddRentalDocumentsDriversAndHolds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(name: "Role", table: "Users", type: "character varying(30)", maxLength: 30, nullable: false, oldClrType: typeof(string), oldType: "character varying(20)", oldMaxLength: 20);
        migrationBuilder.AddColumn<string>(name: "Address", table: "Users", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Avatar", table: "Users", type: "text", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "DateOfBirth", table: "Users", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "IdentityNumber", table: "Users", type: "character varying(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<string>(name: "VerificationStatus", table: "Users", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending");

        migrationBuilder.AddColumn<int>(name: "DriverLicenseId", table: "Bookings", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "IdentityVerificationId", table: "Bookings", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "RentalType", table: "Bookings", type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "SelfDrive");
        migrationBuilder.AddColumn<int>(name: "RentalDays", table: "Bookings", type: "integer", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<decimal>(name: "VehicleRentalPrice", table: "Bookings", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "DriverRentalPrice", table: "Bookings", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>(name: "DriverAssignmentStatus", table: "Bookings", type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "NotRequired");

        migrationBuilder.CreateTable(name: "Drivers", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
            Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
            Avatar = table.Column<string>(type: "text", nullable: true),
            LicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            ExperienceYears = table.Column<int>(type: "integer", nullable: false),
            Description = table.Column<string>(type: "text", nullable: true),
            Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
            PricePerDay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => table.PrimaryKey("PK_Drivers", x => x.Id));

        migrationBuilder.CreateTable(name: "DriverLicenses", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            UserId = table.Column<int>(type: "integer", nullable: false), LicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            LicenseType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false), IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            ExpireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), FrontImage = table.Column<string>(type: "text", nullable: false), BackImage = table.Column<string>(type: "text", nullable: false),
            VerificationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false), RejectionReason = table.Column<string>(type: "text", nullable: true),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => { table.PrimaryKey("PK_DriverLicenses", x => x.Id); table.ForeignKey("FK_DriverLicenses_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade); });

        migrationBuilder.CreateTable(name: "IdentityVerifications", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            UserId = table.Column<int>(type: "integer", nullable: false), CCCDNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
            FrontImage = table.Column<string>(type: "text", nullable: false), BackImage = table.Column<string>(type: "text", nullable: false), Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            RejectionReason = table.Column<string>(type: "text", nullable: true), CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => { table.PrimaryKey("PK_IdentityVerifications", x => x.Id); table.ForeignKey("FK_IdentityVerifications_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade); });

        migrationBuilder.CreateTable(name: "BookingDrivers", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            BookingId = table.Column<int>(type: "integer", nullable: false), DriverId = table.Column<int>(type: "integer", nullable: false), AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_BookingDrivers", x => x.Id); table.ForeignKey("FK_BookingDrivers_Bookings_BookingId", x => x.BookingId, "Bookings", "Id", onDelete: ReferentialAction.Cascade); table.ForeignKey("FK_BookingDrivers_Drivers_DriverId", x => x.DriverId, "Drivers", "Id", onDelete: ReferentialAction.Restrict); });

        migrationBuilder.CreateTable(name: "VehicleReservationHolds", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            VehicleId = table.Column<int>(type: "integer", nullable: false), UserId = table.Column<int>(type: "integer", nullable: false), BookingId = table.Column<int>(type: "integer", nullable: true),
            StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), ExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false), CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_VehicleReservationHolds", x => x.Id); table.ForeignKey("FK_VehicleReservationHolds_Bookings_BookingId", x => x.BookingId, "Bookings", "Id", onDelete: ReferentialAction.Cascade); table.ForeignKey("FK_VehicleReservationHolds_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey("FK_VehicleReservationHolds_Vehicles_VehicleId", x => x.VehicleId, "Vehicles", "Id", onDelete: ReferentialAction.Restrict); });

        migrationBuilder.CreateTable(name: "Notifications", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            UserId = table.Column<int>(type: "integer", nullable: false), Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false), Message = table.Column<string>(type: "text", nullable: false), Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false), IsRead = table.Column<bool>(type: "boolean", nullable: false), CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_Notifications", x => x.Id); table.ForeignKey("FK_Notifications_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade); });

        migrationBuilder.CreateIndex("IX_DriverLicenses_UserId", "DriverLicenses", "UserId", unique: true);
        migrationBuilder.CreateIndex("IX_DriverLicenses_LicenseNumber", "DriverLicenses", "LicenseNumber", unique: true);
        migrationBuilder.CreateIndex("IX_IdentityVerifications_UserId", "IdentityVerifications", "UserId", unique: true);
        migrationBuilder.CreateIndex("IX_IdentityVerifications_CCCDNumber", "IdentityVerifications", "CCCDNumber", unique: true);
        migrationBuilder.CreateIndex("IX_Drivers_LicenseNumber", "Drivers", "LicenseNumber", unique: true);
        migrationBuilder.CreateIndex("IX_BookingDrivers_BookingId", "BookingDrivers", "BookingId", unique: true);
        migrationBuilder.CreateIndex("IX_BookingDrivers_DriverId_StartDate_EndDate", "BookingDrivers", new[] { "DriverId", "StartDate", "EndDate" });
        migrationBuilder.CreateIndex("IX_VehicleReservationHolds_BookingId", "VehicleReservationHolds", "BookingId", unique: true);
        migrationBuilder.CreateIndex("IX_VehicleReservationHolds_UserId", "VehicleReservationHolds", "UserId");
        migrationBuilder.CreateIndex("IX_VehicleReservationHolds_VehicleId_StartDate_EndDate", "VehicleReservationHolds", new[] { "VehicleId", "StartDate", "EndDate" });
        migrationBuilder.CreateIndex("IX_Notifications_UserId_IsRead_CreatedAt", "Notifications", new[] { "UserId", "IsRead", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_Bookings_DriverLicenseId", "Bookings", "DriverLicenseId");
        migrationBuilder.CreateIndex("IX_Bookings_IdentityVerificationId", "Bookings", "IdentityVerificationId");
        migrationBuilder.CreateIndex("IX_Bookings_VehicleId_StartDate_EndDate", "Bookings", new[] { "VehicleId", "StartDate", "EndDate" });
        migrationBuilder.AddForeignKey("FK_Bookings_DriverLicenses_DriverLicenseId", "Bookings", "DriverLicenseId", "DriverLicenses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("FK_Bookings_IdentityVerifications_IdentityVerificationId", "Bookings", "IdentityVerificationId", "IdentityVerifications", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
        migrationBuilder.Sql("ALTER TABLE \"Bookings\" ADD CONSTRAINT \"EX_Bookings_Vehicle_Time\" EXCLUDE USING gist (\"VehicleId\" WITH =, tstzrange(\"StartDate\", \"EndDate\", '[]') WITH &&) WHERE (\"Status\" IN ('PendingPayment','Paid','WaitingApproval','WaitingDriverAssignment','Approved','Pickup','Renting','Returning')); ");
        migrationBuilder.Sql("ALTER TABLE \"VehicleReservationHolds\" ADD CONSTRAINT \"EX_Holds_Vehicle_Time\" EXCLUDE USING gist (\"VehicleId\" WITH =, tstzrange(\"StartDate\", \"EndDate\", '[]') WITH &&) WHERE (\"Status\" = 'Active');");
        migrationBuilder.Sql("ALTER TABLE \"BookingDrivers\" ADD CONSTRAINT \"EX_BookingDrivers_Driver_Time\" EXCLUDE USING gist (\"DriverId\" WITH =, tstzrange(\"StartDate\", \"EndDate\", '[]') WITH &&) WHERE (\"Status\" IN ('Assigned','Accepted')); ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Bookings_DriverLicenses_DriverLicenseId", "Bookings");
        migrationBuilder.DropForeignKey("FK_Bookings_IdentityVerifications_IdentityVerificationId", "Bookings");
        migrationBuilder.DropTable("Notifications"); migrationBuilder.DropTable("VehicleReservationHolds"); migrationBuilder.DropTable("BookingDrivers"); migrationBuilder.DropTable("Drivers");
        migrationBuilder.DropIndex("IX_Bookings_DriverLicenseId", "Bookings"); migrationBuilder.DropIndex("IX_Bookings_IdentityVerificationId", "Bookings"); migrationBuilder.DropIndex("IX_Bookings_VehicleId_StartDate_EndDate", "Bookings");
        migrationBuilder.DropColumn("DriverLicenseId", "Bookings"); migrationBuilder.DropColumn("IdentityVerificationId", "Bookings"); migrationBuilder.DropColumn("RentalType", "Bookings"); migrationBuilder.DropColumn("RentalDays", "Bookings"); migrationBuilder.DropColumn("VehicleRentalPrice", "Bookings"); migrationBuilder.DropColumn("DriverRentalPrice", "Bookings"); migrationBuilder.DropColumn("DriverAssignmentStatus", "Bookings");
        migrationBuilder.DropTable("DriverLicenses"); migrationBuilder.DropTable("IdentityVerifications");
        migrationBuilder.DropColumn("Address", "Users"); migrationBuilder.DropColumn("Avatar", "Users"); migrationBuilder.DropColumn("DateOfBirth", "Users"); migrationBuilder.DropColumn("IdentityNumber", "Users"); migrationBuilder.DropColumn("VerificationStatus", "Users");
        migrationBuilder.AlterColumn<string>(name: "Role", table: "Users", type: "character varying(20)", maxLength: 20, nullable: false, oldClrType: typeof(string), oldType: "character varying(30)", oldMaxLength: 30);
    }
}
