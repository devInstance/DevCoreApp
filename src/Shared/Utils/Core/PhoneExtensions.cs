// Copyright (c) DevInstance LLC. All rights reserved.

using System;
using System.Text;

namespace DevInstance.DevCoreApp.Shared.Utils.Core;

/// <summary>
/// One rule: the database stores a canonical phone string, the UI shows a formatted one,
/// and the decorator is the only place that converts (same contract as DateTimeExtensions).
///
/// Storage form (<see cref="NormalizePhone"/>): E.164-ish — "+15551234567". Digits only, with
/// a leading "+" once a country code is known. A bare 10-digit number is assumed NANP (+1),
/// which matches the display format this app asks for.
///
/// Display form (<see cref="FormatPhone"/>): "(555) 123-4567", or "+44 (207) 183-8750" when
/// the country code is anything other than 1.
///
/// Both are total functions: anything that cannot be interpreted confidently is returned
/// unchanged rather than mangled or discarded.
///
/// The methods are deliberately NOT named Normalize/Format — string already has an instance
/// Normalize() (Unicode) that would silently win over an extension method of the same name.
/// </summary>
public static class PhoneExtensions
{
    /// <summary>
    /// Converts a user-entered phone number to its canonical storage form.
    /// Call this from a decorator's ToRecord — never from a page.
    /// </summary>
    public static string NormalizePhone(this string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var hasPlus = phone.TrimStart().StartsWith('+');
        var digits = PhoneDigits(phone);

        if (digits.Length == 0)
            return string.Empty;

        // Explicit "+" — the caller already told us a country code is present.
        if (hasPlus)
            return "+" + digits;

        // NANP with the trunk/country digit typed in: 1 555 123 4567
        if (digits.Length == 11 && digits[0] == '1')
            return "+" + digits;

        // Bare 10-digit number — assume NANP, consistent with the display format.
        if (digits.Length == 10)
            return "+1" + digits;

        // 7-digit local numbers, short codes, or a length we cannot attribute to a country.
        // Keep the digits; do not invent a country code.
        return digits;
    }

    /// <summary>
    /// Null-preserving <see cref="NormalizePhone"/>, for nullable phone columns — a blank
    /// input stays null rather than becoming an empty string.
    /// </summary>
    public static string? NormalizePhoneOrNull(this string? phone)
        => string.IsNullOrWhiteSpace(phone) ? null : NormalizePhone(phone);

    /// <summary>
    /// Digits only, no "+" — the search-index form stored in NormalizedValue, and the form a
    /// search term must be reduced to before it is matched against that column.
    /// </summary>
    public static string PhoneDigits(this string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var sb = new StringBuilder(phone.Length);
        foreach (var c in phone)
        {
            if (char.IsAsciiDigit(c))
                sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts a stored phone number to its display form. Call this from a decorator's
    /// ToView — never from a page. Tolerates un-normalized legacy values, so rows written
    /// before normalization was introduced still render correctly.
    /// </summary>
    public static string FormatPhone(this string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = PhoneDigits(phone);
        if (digits.Length == 0)
            return phone;

        var explicitCountry = phone.TrimStart().StartsWith('+');

        // NANP, with or without the leading 1 — country code 1 is dropped from display.
        if (digits.Length == 10)
            return FormatNanp(digits);
        if (digits.Length == 11 && digits[0] == '1')
            return FormatNanp(digits[1..]);

        // Anything longer with an explicit "+" is read as <country code><10 subscriber digits>.
        if (explicitCountry && digits.Length > 11)
            return $"+{digits[..^10]} {FormatNanp(digits[^10..])}";

        // 7-digit local number.
        if (digits.Length == 7)
            return $"{digits[..3]}-{digits[3..]}";

        // Short codes, unknown lengths, non-"+" international — leave exactly as stored.
        return phone;
    }

    private static string FormatNanp(string tenDigits)
        => $"({tenDigits[..3]}) {tenDigits[3..6]}-{tenDigits[6..]}";
}
