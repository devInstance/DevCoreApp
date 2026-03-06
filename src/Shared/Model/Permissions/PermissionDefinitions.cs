using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DevInstance.DevCoreApp.Shared.Model.Permissions;

public static class PermissionDefinitions
{
    public static class Admin
    {
        public static class Users
        {
            public const string View = "Admin.Users.View";
            public const string Create = "Admin.Users.Create";
            public const string Edit = "Admin.Users.Edit";
            public const string Delete = "Admin.Users.Delete";
            public const string Impersonate = "Admin.Users.Impersonate";
        }

        public static class Roles
        {
            public const string View = "Admin.Roles.View";
            public const string Create = "Admin.Roles.Create";
            public const string Edit = "Admin.Roles.Edit";
            public const string Delete = "Admin.Roles.Delete";
        }

        public static class Settings
        {
            public const string View = "Admin.Settings.View";
            public const string Edit = "Admin.Settings.Edit";
        }

        public static class Organizations
        {
            public const string View = "Admin.Organizations.View";
            public const string Create = "Admin.Organizations.Create";
            public const string Edit = "Admin.Organizations.Edit";
            public const string Delete = "Admin.Organizations.Delete";
        }

        public static class FeatureFlags
        {
            public const string View = "Admin.FeatureFlags.View";
            public const string Create = "Admin.FeatureFlags.Create";
            public const string Edit = "Admin.FeatureFlags.Edit";
            public const string Delete = "Admin.FeatureFlags.Delete";
        }

        public static class ApiKeys
        {
            public const string View = "Admin.ApiKeys.View";
            public const string Create = "Admin.ApiKeys.Create";
            public const string Revoke = "Admin.ApiKeys.Revoke";
        }

        public static class Webhooks
        {
            public const string View = "Admin.Webhooks.View";
            public const string Create = "Admin.Webhooks.Create";
            public const string Edit = "Admin.Webhooks.Edit";
            public const string Delete = "Admin.Webhooks.Delete";
        }
    }

    public static class System
    {
        public static class Jobs
        {
            public const string View = "System.Jobs.View";
            public const string Cancel = "System.Jobs.Cancel";
            public const string Retry = "System.Jobs.Retry";
        }

        public static class EmailLog
        {
            public const string View = "System.EmailLog.View";
            public const string Resend = "System.EmailLog.Resend";
        }

        public static class AuditLog
        {
            public const string View = "System.AuditLog.View";
        }
    }

    /// <summary>
    /// Returns all permission key strings defined in this class via reflection.
    /// Walks nested static classes and collects every public const string field.
    /// </summary>
    public static IReadOnlyList<string> GetAll()
    {
        return CollectConstants(typeof(PermissionDefinitions))
            .OrderBy(k => k)
            .ToList();
    }

    private static IEnumerable<string> CollectConstants(global::System.Type type)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            {
                var value = (string)field.GetRawConstantValue()!;
                yield return value;
            }
        }

        foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            foreach (var value in CollectConstants(nested))
            {
                yield return value;
            }
        }
    }
}
