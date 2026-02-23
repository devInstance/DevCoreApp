using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Base;

/// <summary>
/// Properties decorated with this attribute are omitted from audit log
/// OldValues/NewValues JSON. Use on sensitive fields such as PasswordHash,
/// SecurityStamp, or any field containing secrets.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AuditExcludeAttribute : Attribute
{
}
