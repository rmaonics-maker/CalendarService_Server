using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calendar_Service.Migrations
{
    /// <inheritdoc />
    public partial class TimeStampWithTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "events",
                newName: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "events",
                newName: "created_at_utc");
        }
    }
}
