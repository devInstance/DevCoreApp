// Copyright (c) DevInstance LLC. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace DevInstance.DevCoreApp.Shared.Model.Core.Common;

/// <summary>
/// Validates a URL only when a value is present, and assumes <c>https</c> when the user omits the
/// scheme — so <c>example.com</c> is accepted. Unlike <see cref="UrlAttribute"/>, a null, empty,
/// or whitespace value is treated as valid, so clearing an optional URL field in a form does not
/// flag it as invalid.
///
/// The matching write-side conversion is <see cref="UrlExtensions.NormalizeUrlOrNull"/>, called from
/// the decorator — this attribute only accepts the scheme-less form, it does not store it.
/// </summary>
public sealed class OptionalUrlAttribute : ValidationAttribute
{
    public OptionalUrlAttribute()
    {
        ErrorMessage = "The {0} field is not a valid URL.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string s)
            return true;

        return UrlExtensions.IsValidOptionalUrl(s);
    }
}
