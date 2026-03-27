using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class FixesFromThreadIQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "UserProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "GridProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "GridProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "EmailLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "EmailLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                table: "ApiKeys",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "ApiKeys",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CreatedById",
                table: "Organizations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_UpdatedById",
                table: "Organizations",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_GridProfiles_CreatedById",
                table: "GridProfiles",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_GridProfiles_UpdatedById",
                table: "GridProfiles",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_CreatedById",
                table: "EmailLogs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_UpdatedById",
                table: "EmailLogs",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UpdatedById",
                table: "ApiKeys",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_UserProfiles_UpdatedById",
                table: "ApiKeys",
                column: "UpdatedById",
                principalTable: "UserProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailLogs_UserProfiles_CreatedById",
                table: "EmailLogs",
                column: "CreatedById",
                principalTable: "UserProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailLogs_UserProfiles_UpdatedById",
                table: "EmailLogs",
                column: "UpdatedById",
                principalTable: "UserProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GridProfiles_UserProfiles_CreatedById",
                table: "GridProfiles",
                column: "CreatedById",
                principalTable: "UserProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GridProfiles_UserProfiles_UpdatedById",
                table: "GridProfiles",
                column: "UpdatedById",
                principalTable: "UserProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_UserProfiles_CreatedById",
                table: "Organizations",
                column: "CreatedById",
                principalTable: "UserProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_UserProfiles_UpdatedById",
                table: "Organizations",
                column: "UpdatedById",
                principalTable: "UserProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_UserProfiles_UpdatedById",
                table: "ApiKeys");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailLogs_UserProfiles_CreatedById",
                table: "EmailLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailLogs_UserProfiles_UpdatedById",
                table: "EmailLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_GridProfiles_UserProfiles_CreatedById",
                table: "GridProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_GridProfiles_UserProfiles_UpdatedById",
                table: "GridProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_UserProfiles_CreatedById",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_UserProfiles_UpdatedById",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_CreatedById",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_UpdatedById",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_GridProfiles_CreatedById",
                table: "GridProfiles");

            migrationBuilder.DropIndex(
                name: "IX_GridProfiles_UpdatedById",
                table: "GridProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmailLogs_CreatedById",
                table: "EmailLogs");

            migrationBuilder.DropIndex(
                name: "IX_EmailLogs_UpdatedById",
                table: "EmailLogs");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_UpdatedById",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "GridProfiles");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "GridProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "ApiKeys");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                table: "ApiKeys",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
