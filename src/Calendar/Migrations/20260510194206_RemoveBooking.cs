using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Calendar_Service.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    period = table.Column<NpgsqlRange<DateTime>>(type: "tstzrange", nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recurrent_rule = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_events_user_id",
                table: "events",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<NpgsqlRange<DateTime>>(type: "tsrange", nullable: false),
                    room_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_period",
                table: "bookings",
                column: "period")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_user_id",
                table: "bookings",
                column: "user_id");
        }
    }
}
