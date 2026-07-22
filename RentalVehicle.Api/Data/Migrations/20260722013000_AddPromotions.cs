using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RentalVehicle.Api.Data.Migrations;

public partial class AddPromotions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Promotions",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Subtitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Badge = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                BadgeColor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                PromoCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Promotions", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Promotions_IsActive_StartDate_EndDate",
            table: "Promotions",
            columns: new[] { "IsActive", "StartDate", "EndDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Promotions_SortOrder",
            table: "Promotions",
            column: "SortOrder");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Promotions");
    }
}
