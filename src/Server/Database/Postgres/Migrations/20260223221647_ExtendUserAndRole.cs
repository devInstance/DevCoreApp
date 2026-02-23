using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ExtendUserAndRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrimaryOrganizationId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AspNetRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                table: "AspNetRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PrimaryOrganizationId",
                table: "AspNetUsers",
                column: "PrimaryOrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Organizations_PrimaryOrganizationId",
                table: "AspNetUsers",
                column: "PrimaryOrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizations_PrimaryOrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PrimaryOrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PrimaryOrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                table: "AspNetRoles");
        }
    }
}
