using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calendar.Booking.API.Migrations
{
    /// <inheritdoc />
    public partial class TimeStampWithTimaZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "bookings",
                newName: "created_at");

            migrationBuilder.AddColumn<Guid>(
                name: "appointment_id",
                table: "bookings",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "appointment_id",
                table: "bookings");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "bookings",
                newName: "created_at_utc");
        }
    }
}
