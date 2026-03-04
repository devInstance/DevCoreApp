using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ImportUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImportedRecordIdsJson",
                table: "ImportSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedRows",
                table: "ImportSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImportedRecordIdsJson",
                table: "ImportSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedRows",
                table: "ImportSessions");
        }
    }
}
