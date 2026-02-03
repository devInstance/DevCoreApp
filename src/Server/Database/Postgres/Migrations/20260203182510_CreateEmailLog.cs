using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class CreateEmailLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromAddress = table.Column<string>(type: "text", nullable: true),
                    FromName = table.Column<string>(type: "text", nullable: true),
                    ToAddress = table.Column<string>(type: "text", nullable: true),
                    ToName = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    IsHtml = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TemplateName = table.Column<string>(type: "text", nullable: true),
                    PublicId = table.Column<string>(type: "text", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLogs");
        }
    }
}
