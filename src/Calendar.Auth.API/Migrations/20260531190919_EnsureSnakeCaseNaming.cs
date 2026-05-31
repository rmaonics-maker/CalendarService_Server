using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calendar.Auth.API.Migrations
{
    /// <inheritdoc />
    public partial class EnsureSnakeCaseNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_open_iddict_authorizations_open_iddict_applications_application",
                table: "OpenIddictAuthorizations");

            migrationBuilder.DropForeignKey(
                name: "fk_open_iddict_tokens_open_iddict_authorizations_authorization_id",
                table: "OpenIddictTokens");

            migrationBuilder.RenameTable(
                name: "OpenIddictTokens",
                newName: "open_iddict_tokens");

            migrationBuilder.RenameTable(
                name: "OpenIddictScopes",
                newName: "open_iddict_scopes");

            migrationBuilder.RenameTable(
                name: "OpenIddictAuthorizations",
                newName: "open_iddict_authorizations");

            migrationBuilder.RenameTable(
                name: "OpenIddictApplications",
                newName: "open_iddict_applications");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "asp_net_user_tokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "asp_net_users");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "asp_net_user_roles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "asp_net_user_logins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "asp_net_user_claims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "asp_net_roles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "asp_net_role_claims");

            migrationBuilder.RenameIndex(
                name: "ix_open_iddict_authorizations_application_id_status_subject_type",
                table: "open_iddict_authorizations",
                newName: "ix_open_iddict_authorizations_application_id_status_subject_ty");

            migrationBuilder.RenameIndex(
                name: "UserNameIndex",
                table: "asp_net_users",
                newName: "user_name_index");

            migrationBuilder.RenameIndex(
                name: "EmailIndex",
                table: "asp_net_users",
                newName: "email_index");

            migrationBuilder.RenameIndex(
                name: "RoleNameIndex",
                table: "asp_net_roles",
                newName: "role_name_index");

            migrationBuilder.AddForeignKey(
                name: "fk_open_iddict_authorizations_open_iddict_applications_applica",
                table: "open_iddict_authorizations",
                column: "application_id",
                principalTable: "open_iddict_applications",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_open_iddict_tokens_open_iddict_authorizations_authorization",
                table: "open_iddict_tokens",
                column: "authorization_id",
                principalTable: "open_iddict_authorizations",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_open_iddict_authorizations_open_iddict_applications_applica",
                table: "open_iddict_authorizations");

            migrationBuilder.DropForeignKey(
                name: "fk_open_iddict_tokens_open_iddict_authorizations_authorization",
                table: "open_iddict_tokens");

            migrationBuilder.RenameTable(
                name: "open_iddict_tokens",
                newName: "OpenIddictTokens");

            migrationBuilder.RenameTable(
                name: "open_iddict_scopes",
                newName: "OpenIddictScopes");

            migrationBuilder.RenameTable(
                name: "open_iddict_authorizations",
                newName: "OpenIddictAuthorizations");

            migrationBuilder.RenameTable(
                name: "open_iddict_applications",
                newName: "OpenIddictApplications");

            migrationBuilder.RenameTable(
                name: "asp_net_users",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "asp_net_user_tokens",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "asp_net_user_roles",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "asp_net_user_logins",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "asp_net_user_claims",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "asp_net_roles",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "asp_net_role_claims",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameIndex(
                name: "ix_open_iddict_authorizations_application_id_status_subject_ty",
                table: "OpenIddictAuthorizations",
                newName: "ix_open_iddict_authorizations_application_id_status_subject_type");

            migrationBuilder.RenameIndex(
                name: "user_name_index",
                table: "AspNetUsers",
                newName: "UserNameIndex");

            migrationBuilder.RenameIndex(
                name: "email_index",
                table: "AspNetUsers",
                newName: "EmailIndex");

            migrationBuilder.RenameIndex(
                name: "role_name_index",
                table: "AspNetRoles",
                newName: "RoleNameIndex");

            migrationBuilder.AddForeignKey(
                name: "fk_open_iddict_authorizations_open_iddict_applications_application",
                table: "OpenIddictAuthorizations",
                column: "application_id",
                principalTable: "OpenIddictApplications",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_open_iddict_tokens_open_iddict_authorizations_authorization_id",
                table: "OpenIddictTokens",
                column: "authorization_id",
                principalTable: "OpenIddictAuthorizations",
                principalColumn: "id");
        }
    }
}
