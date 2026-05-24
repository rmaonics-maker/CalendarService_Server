using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using NpgsqlTypes;

#nullable disable

namespace Calendar_Service.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentException : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "period",
                table: "events");

            migrationBuilder.AddColumn<string>(
                name: "date_time_zone_id",
                table: "events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Instant>(
                name: "end_at",
                table: "events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.AddColumn<int>(
                name: "recurrent_count",
                table: "events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "recurrent_until",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "start_at",
                table: "events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.CreateTable(
                name: "event_exceptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    master_appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_occurrence_date = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    is_cancelled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    override_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    override_end_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    override_start_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_exceptions_events_master_appointment_id",
                        column: x => x.master_appointment_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_exceptions_master_appointment_id",
                table: "event_exceptions",
                column: "master_appointment_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_exceptions_master_appointment_id_original_occurrence_",
                table: "event_exceptions",
                columns: new[] { "master_appointment_id", "original_occurrence_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_exceptions");

            migrationBuilder.DropColumn(
                name: "date_time_zone_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "end_at",
                table: "events");

            migrationBuilder.DropColumn(
                name: "recurrent_count",
                table: "events");

            migrationBuilder.DropColumn(
                name: "recurrent_until",
                table: "events");

            migrationBuilder.DropColumn(
                name: "start_at",
                table: "events");

            migrationBuilder.AddColumn<NpgsqlRange<DateTime>>(
                name: "period",
                table: "events",
                type: "tstzrange",
                nullable: false,
                defaultValue: new NpgsqlTypes.NpgsqlRange<System.DateTime>(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), false));
        }
    }
}
