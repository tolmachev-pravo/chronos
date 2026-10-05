using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShortenPreHolidayDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShortenPreHolidayDays",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortenPreHolidayDays",
                table: "UserSettings");
        }
    }
}
