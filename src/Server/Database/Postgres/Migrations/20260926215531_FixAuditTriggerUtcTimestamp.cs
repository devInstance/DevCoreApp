using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class FixAuditTriggerUtcTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // audit_trigger_function() stored NOW() AT TIME ZONE 'UTC' into the timestamptz
            // ChangedAt column, which Postgres re-reads in the session time zone: every
            // database-sourced audit row was shifted by the session's UTC offset. Re-create the
            // function from the corrected helper (CREATE OR REPLACE — idempotent).
            // Rows written before this migration keep their shifted timestamps.
            migrationBuilder.CreateAuditTriggerFunction();
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: restoring the previous function would reintroduce the bug.
        }
    }
}
