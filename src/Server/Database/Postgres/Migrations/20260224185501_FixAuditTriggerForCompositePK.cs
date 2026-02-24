using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Migrations
{
    /// <summary>
    /// Fixes audit_trigger_function() to handle tables without an "Id" column
    /// (e.g., RolePermissions which uses a composite primary key).
    /// The updated function checks for the "Id" key in the JSONB representation
    /// and falls back to the full row JSON when "Id" is absent.
    /// </summary>
    public partial class FixAuditTriggerForCompositePK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CREATE OR REPLACE — overwrites the existing function in place.
            // All existing triggers that reference audit_trigger_function() will
            // automatically pick up the new definition.
            migrationBuilder.CreateAuditTriggerFunction();
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the original function that assumes an "Id" column exists.
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION audit_trigger_function()
RETURNS TRIGGER AS $$
DECLARE
    audit_action INTEGER;
    old_values JSONB := NULL;
    new_values JSONB := NULL;
    record_id TEXT := '';
BEGIN
    IF TG_OP = 'INSERT' THEN
        audit_action := 0;
        new_values := to_jsonb(NEW);
        record_id := NEW.""Id""::TEXT;
    ELSIF TG_OP = 'UPDATE' THEN
        audit_action := 1;
        old_values := to_jsonb(OLD);
        new_values := to_jsonb(NEW);
        record_id := NEW.""Id""::TEXT;
    ELSIF TG_OP = 'DELETE' THEN
        audit_action := 2;
        old_values := to_jsonb(OLD);
        record_id := OLD.""Id""::TEXT;
    END IF;

    INSERT INTO ""AuditLogs"" (
        ""Id"",
        ""OrganizationId"",
        ""TableName"",
        ""RecordId"",
        ""Action"",
        ""OldValues"",
        ""NewValues"",
        ""ChangedByUserId"",
        ""ChangedAt"",
        ""IpAddress"",
        ""CorrelationId"",
        ""Source""
    ) VALUES (
        gen_random_uuid(),
        NULL,
        TG_TABLE_NAME,
        record_id,
        audit_action,
        old_values::TEXT,
        new_values::TEXT,
        NULL,
        NOW() AT TIME ZONE 'UTC',
        NULL,
        NULL,
        1
    );

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    ELSE
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql;
");
        }
    }
}
