// Copyright (c) DevInstance LLC. All rights reserved.

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DevInstance.DevCoreApp.Shared.Model.Core.Common;

/// <summary>
/// URL storage/display rules for user-entered URL fields (Website, LinkedIn, Facebook, …).
/// Users type a bare host — <c>example.com</c> — so a missing scheme is assumed to be
/// <c>https</c> rather than rejected. Storage always keeps the scheme-qualified form, which is
/// what an <c>&lt;a href&gt;</c> needs (a scheme-less value would render as a relative link).
///
/// Same shape as <c>PhoneExtensions</c>: the decorator is the only place that converts, and the
/// conversion is total — anything that already carries a scheme is returned unchanged, never mangled.
///
/// This lives in Shared.Model rather than beside PhoneExtensions in Shared.Utils because
/// <see cref="OptionalUrlAttribute"/> needs it and Shared.Model has no project references.
/// </summary>
public static class UrlExtensions
{
    private const string DefaultScheme = "https://";

    /// <summary>Schemes a user-entered web URL may use. Anything else (<c>mailto:</c>, <c>tel:</c>) is not a website.</summary>
    private static readonly string[] AllowedSchemes = { Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeFtp };

    /// <summary>An explicit scheme with an authority — <c>https://…</c>, <c>ftp://…</c>.</summary>
    private static readonly Regex SchemeWithAuthority =
        new(@"^[a-zA-Z][a-zA-Z0-9+.\-]*://", RegexOptions.Compiled);

    /// <summary>
    /// An explicit scheme without an authority — <c>mailto:…</c>, <c>tel:…</c>. The negative lookahead
    /// keeps <c>example.com:8080</c> out: a colon followed by digits is a port, not a scheme, and that
    /// value still needs the assumed <c>https://</c>.
    /// </summary>
    private static readonly Regex SchemeWithoutAuthority =
        new(@"^[a-zA-Z][a-zA-Z0-9+.\-]*:(?!\d)", RegexOptions.Compiled);

    /// <summary>
    /// User input → storage/display form: trims and prefixes <c>https://</c> when no scheme is present.
    /// Null/blank is returned as null so clearing an optional field stays null.
    /// </summary>
    public static string? NormalizeUrlOrNull(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        if (SchemeWithAuthority.IsMatch(trimmed) || SchemeWithoutAuthority.IsMatch(trimmed))
            return trimmed;

        return DefaultScheme + trimmed;
    }

    /// <summary>
    /// True when the value is blank (nothing to validate) or is a usable web URL once the assumed
    /// <c>https</c> scheme is applied. This is what <see cref="OptionalUrlAttribute"/> checks.
    /// </summary>
    public static bool IsValidOptionalUrl(string? value)
    {
        var normalized = value.NormalizeUrlOrNull();
        if (normalized is null)
            return true;

        // A URL a user can paste into a browser has no unescaped whitespace; Uri would silently
        // escape it and accept "https://not a url".
        if (normalized.Any(char.IsWhiteSpace))
            return false;

        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && AllowedSchemes.Contains(uri.Scheme)
            && !string.IsNullOrEmpty(uri.Host);
    }
}
