using Microsoft.EntityFrameworkCore.Migrations;

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Migrations;

/// <summary>
/// Reusable migration helpers for managing the PostgreSQL audit trigger function
/// and attaching/detaching it to/from tables. Use these in any migration that
/// creates a new critical table requiring tamper-proof auditing.
///
/// Usage in a migration:
///   migrationBuilder.AttachAuditTrigger("MyNewTable");
///   migrationBuilder.DetachAuditTrigger("MyNewTable");
/// </summary>
public static class AuditTriggerExtensions
{
    /// <summary>
    /// Creates the shared audit_trigger_function() in the database.
    /// Call this once (idempotent via CREATE OR REPLACE).
    /// </summary>
    public static void CreateAuditTriggerFunction(this MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION audit_trigger_function()
RETURNS TRIGGER AS $$
DECLARE
    audit_action INTEGER;
    old_values JSONB := NULL;
    new_values JSONB := NULL;
    record_id TEXT := '';
    record_data JSONB;
BEGIN
    -- Map TG_OP to AuditAction enum values: Insert=0, Update=1, Delete=2
    IF TG_OP = 'INSERT' THEN
        audit_action := 0;
        new_values := to_jsonb(NEW);
        record_data := new_values;
    ELSIF TG_OP = 'UPDATE' THEN
        audit_action := 1;
        old_values := to_jsonb(OLD);
        new_values := to_jsonb(NEW);
        record_data := new_values;
    ELSIF TG_OP = 'DELETE' THEN
        audit_action := 2;
        old_values := to_jsonb(OLD);
        record_data := old_values;
    END IF;

    -- Extract record ID: use ""Id"" column if present, otherwise build
    -- a composite key from all primary key columns via JSONB representation.
    IF record_data ? 'Id' THEN
        record_id := record_data->>'Id';
    ELSE
        record_id := record_data::TEXT;
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
        1  -- AuditSource.Database = 1
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

    /// <summary>
    /// Drops the shared audit_trigger_function() from the database.
    /// </summary>
    public static void DropAuditTriggerFunction(this MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS audit_trigger_function() CASCADE;");
    }

    /// <summary>
    /// Attaches the audit trigger to a table. Creates a BEFORE trigger for
    /// INSERT, UPDATE, and DELETE operations.
    /// Call this in any migration that creates a critical table.
    /// </summary>
    public static void AttachAuditTrigger(this MigrationBuilder migrationBuilder, string tableName)
    {
        var triggerName = $"trg_audit_{tableName.ToLowerInvariant()}";
        migrationBuilder.Sql($@"
CREATE TRIGGER ""{triggerName}""
    AFTER INSERT OR UPDATE OR DELETE ON ""{tableName}""
    FOR EACH ROW EXECUTE FUNCTION audit_trigger_function();
");
    }

    /// <summary>
    /// Detaches the audit trigger from a table.
    /// Call this in the Down() method of the migration that attached it.
    /// </summary>
    public static void DetachAuditTrigger(this MigrationBuilder migrationBuilder, string tableName)
    {
        var triggerName = $"trg_audit_{tableName.ToLowerInvariant()}";
        migrationBuilder.Sql($@"DROP TRIGGER IF EXISTS ""{triggerName}"" ON ""{tableName}"";");
    }
}
