using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calendar_Service.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "username",
                table: "users",
                newName: "name");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "users",
                newName: "username");
        }
    }
}
