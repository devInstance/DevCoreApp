using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgHierarchyAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Organizations ---
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Settings = table.Column<string>(type: "jsonb", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PublicId = table.Column<string>(type: "text", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_Organizations_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Path",
                table: "Organizations",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Code",
                table: "Organizations",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_ParentId",
                table: "Organizations",
                column: "ParentId");

            // --- Tenants ---
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Subdomain = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Plan = table.Column<string>(type: "text", nullable: false),
                    Settings = table.Column<string>(type: "jsonb", nullable: true),
                    RootOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublicId = table.Column<string>(type: "text", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_Organizations_RootOrganizationId",
                        column: x => x.RootOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tenants_UserProfiles_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "UserProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tenants_UserProfiles_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "UserProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_RootOrganizationId",
                table: "Tenants",
                column: "RootOrganizationId",
                unique: true);

            // --- UserOrganizations ---
            migrationBuilder.CreateTable(
                name: "UserOrganizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrganizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOrganizations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserOrganizations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizations_UserId_OrganizationId",
                table: "UserOrganizations",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true);

            // --- AuditLogs ---
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    TableName = table.Column<string>(type: "text", nullable: false),
                    RecordId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TableName_RecordId_ChangedAt",
                table: "AuditLogs",
                columns: new[] { "TableName", "RecordId", "ChangedAt" });

            // --- Audit Trigger Function + Triggers on Critical Tables ---
            migrationBuilder.CreateAuditTriggerFunction();

            // Attach to tables that exist now.
            // RolePermissions and UserPermissionOverrides will be created in a later phase;
            // those migrations should call migrationBuilder.AttachAuditTrigger("TableName").
            migrationBuilder.AttachAuditTrigger("AspNetUsers");
            migrationBuilder.AttachAuditTrigger("Organizations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Detach triggers before dropping tables/function
            migrationBuilder.DetachAuditTrigger("Organizations");
            migrationBuilder.DetachAuditTrigger("AspNetUsers");
            migrationBuilder.DropAuditTriggerFunction();

            migrationBuilder.DropTable(name: "AuditLogs");
            migrationBuilder.DropTable(name: "UserOrganizations");
            migrationBuilder.DropTable(name: "Tenants");
            migrationBuilder.DropTable(name: "Organizations");
        }
    }
}
