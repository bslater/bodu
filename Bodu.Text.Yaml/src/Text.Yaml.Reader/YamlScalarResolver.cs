// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlScalarResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Applies the implicit type resolution rules of the YAML core (1.2) and 1.1 schemas to a plain (unquoted) scalar,
/// classifying it as null, boolean, integer, or floating-point, or leaving it as a string.
/// </summary>
/// <remarks>
/// Only plain scalars are subject to resolution. Single-quoted, double-quoted, and block scalars are always strings and
/// must not be passed to this resolver. The 1.2 core schema deliberately recognizes only <c>true</c>/<c>false</c> as
/// booleans; the 1.1 schema additionally recognizes <c>yes</c>/<c>no</c>, <c>on</c>/<c>off</c>, and <c>y</c>/<c>n</c>,
/// which is the source of the well-known "Norway problem". Resolution is allocation-free: the numeric attempts share
/// one stack transcode of the scalar bytes rather than materializing strings.
/// </remarks>
internal static class YamlScalarResolver
{
    /// <summary>The scalar length at or below which the numeric transcode uses a stack buffer instead of a heap array.</summary>
    private const int StackallocCharThreshold = 128;

    /// <summary>
    /// Resolves the type of a plain scalar and, for non-string kinds, produces its packed numeric or boolean payload.
    /// </summary>
    /// <param name="text">The raw UTF-8 bytes of the plain scalar's content.</param>
    /// <param name="version">The specification version whose resolution rules are applied.</param>
    /// <param name="bits">
    /// When the result is <see cref="YamlValueKind.Boolean" />, <see cref="YamlValueKind.Integer" />, or
    /// <see cref="YamlValueKind.Float" />, the packed payload; otherwise zero.
    /// </param>
    /// <returns>The resolved value kind. <see cref="YamlValueKind.String" /> when no implicit type applies.</returns>
    internal static YamlValueKind Resolve(ReadOnlySpan<byte> text, YamlSpecVersion version, out long bits)
    {
        bits = 0;

        if (text.Length == 0 || IsNull(text))
            return YamlValueKind.Null;

        if (TryResolveBoolean(text, version, out bool boolValue))
        {
            bits = boolValue ? 1L : 0L;
            return YamlValueKind.Boolean;
        }

        // The integer and float attempts share one transcode: a scalar carrying a non-ASCII byte cannot be numeric, so
        // resolution short-circuits to a string without allocating.
        Span<char> buffer = text.Length <= StackallocCharThreshold ? stackalloc char[StackallocCharThreshold] : new char[text.Length];
        int length = TranscodeNumericCandidate(text, buffer);
        if (length < 0)
            return YamlValueKind.String;

        Span<char> candidate = buffer[..length];

        if (TryResolveInteger(candidate, version, out long longValue))
        {
            bits = longValue;
            return YamlValueKind.Integer;
        }

        // The integer forms are matched with their underscores in place, because YAML 1.1 groups digits only after the
        // first one; the float attempt reads the 1.1 schema's digit groups with the separators removed, once it has
        // ruled out digits that begin with one, since every 1.1 float form begins, after any sign, with a digit or the
        // decimal point.
        if (version == YamlSpecVersion.V1_1)
        {
            if (BeginsWithDigitSeparator(candidate))
                return YamlValueKind.String;

            candidate = candidate[..RemoveDigitSeparators(candidate)];
        }

        if (TryResolveFloat(candidate, out double doubleValue))
        {
            bits = BitConverter.DoubleToInt64Bits(doubleValue);
            return YamlValueKind.Float;
        }

        return YamlValueKind.String;
    }

    /// <summary>
    /// Determines whether a plain scalar resolves to the YAML null value.
    /// </summary>
    /// <param name="text">The raw UTF-8 bytes of the scalar.</param>
    /// <returns>
    /// <see langword="true" /> when the scalar is the null value; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsNull(ReadOnlySpan<byte> text) =>
        (text.Length == 1 && text[0] == (byte)'~')
        || Matches(text, "null") || Matches(text, "Null") || Matches(text, "NULL");

    /// <summary>
    /// Attempts to resolve a plain scalar as a boolean under the active schema.
    /// </summary>
    /// <param name="text">The raw UTF-8 bytes of the scalar.</param>
    /// <param name="version">The specification version whose boolean spellings apply.</param>
    /// <param name="value">When the method returns <see langword="true" />, the resolved boolean.</param>
    /// <returns><see langword="true" /> when the scalar is a boolean; otherwise <see langword="false" />.</returns>
    private static bool TryResolveBoolean(ReadOnlySpan<byte> text, YamlSpecVersion version, out bool value)
    {
        if (Matches(text, "true") || Matches(text, "True") || Matches(text, "TRUE"))
        {
            value = true;
            return true;
        }

        if (Matches(text, "false") || Matches(text, "False") || Matches(text, "FALSE"))
        {
            value = false;
            return true;
        }

        if (version == YamlSpecVersion.V1_1)
        {
            if (MatchesAny(text, "yes", "Yes", "YES", "on", "On", "ON", "y", "Y"))
            {
                value = true;
                return true;
            }

            if (MatchesAny(text, "no", "No", "NO", "off", "Off", "OFF", "n", "N"))
            {
                value = false;
                return true;
            }
        }

        value = false;
        return false;
    }

    /// <summary>
    /// Transcodes a scalar's bytes into the numeric-candidate characters both numeric attempts parse.
    /// </summary>
    /// <param name="text">The raw UTF-8 bytes of the scalar.</param>
    /// <param name="destination">The buffer that receives the characters; at least the scalar's length.</param>
    /// <returns>
    /// The number of characters written, or <c>-1</c> when a non-ASCII byte makes the scalar non-numeric.
    /// </returns>
    private static int TranscodeNumericCandidate(ReadOnlySpan<byte> text, Span<char> destination)
    {
        int n = 0;
        foreach (byte b in text)
        {
            if (b >= 0x80)
                return -1;

            destination[n++] = (char)b;
        }

        return n;
    }

    /// <summary>
    /// Determines whether a numeric candidate's digits, after any sign, begin with an underscore, which no YAML 1.1
    /// numeric form allows.
    /// </summary>
    /// <param name="candidate">The candidate characters.</param>
    /// <returns>
    /// <see langword="true" /> when the first character after any sign is an underscore; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool BeginsWithDigitSeparator(ReadOnlySpan<char> candidate)
    {
        ReadOnlySpan<char> body = candidate.Length > 0 && candidate[0] is '+' or '-' ? candidate[1..] : candidate;
        return body.Length > 0 && body[0] == '_';
    }

    /// <summary>
    /// Removes the YAML 1.1 schema's underscore digit-group separators from a numeric candidate in place.
    /// </summary>
    /// <param name="candidate">The candidate characters, compacted in place.</param>
    /// <returns>The number of characters that remain at the start of <paramref name="candidate" />.</returns>
    private static int RemoveDigitSeparators(Span<char> candidate)
    {
        int n = 0;
        for (int i = 0; i < candidate.Length; i++)
        {
            if (candidate[i] != '_')
                candidate[n++] = candidate[i];
        }

        return n;
    }

    /// <summary>
    /// Attempts to resolve a numeric candidate as a 64-bit integer under the active schema.
    /// </summary>
    /// <param name="s">The candidate characters, transcoded by <see cref="TranscodeNumericCandidate" />.</param>
    /// <param name="version">The specification version whose integer forms apply.</param>
    /// <param name="value">When the method returns <see langword="true" />, the resolved integer.</param>
    /// <returns>
    /// <see langword="true" /> when the scalar is an integer that fits in a 64-bit signed integer; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryResolveInteger(ReadOnlySpan<char> s, YamlSpecVersion version, out long value)
    {
        value = 0;

        if (s.Length == 0)
            return false;

        bool negative = false;
        bool hasSign = false;
        ReadOnlySpan<char> body = s;
        if (s[0] is '+' or '-')
        {
            hasSign = true;
            negative = s[0] == '-';
            body = s[1..];
            if (body.Length == 0)
                return false;
        }

        // The core schema's 0o octal takes no sign or digit groups, and YAML 1.1 keeps it in that form; a signed or
        // grouped 0o literal is not an integer and remains a string.
        if (!hasSign && body.StartsWith("0o", StringComparison.Ordinal))
            return TryParseRadix(body[2..], 8, negative, grouped: false, out value);

        if (version == YamlSpecVersion.V1_1)
            return TryResolveYaml11Integer(body, negative, out value);

        // The core schema's 0x hexadecimal takes no sign either, so a signed hexadecimal literal remains a string.
        if (!hasSign && body.StartsWith("0x", StringComparison.Ordinal))
            return TryParseRadix(body[2..], 16, negative, grouped: false, out value);

        if (!IsAllDigits(body, 10))
            return false;

        return long.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Attempts to resolve an unsigned candidate body as one of the YAML 1.1 int type's forms: binary (<c>0b</c>),
    /// hexadecimal (<c>0x</c>), leading-zero octal, or decimal.
    /// </summary>
    /// <param name="body">The candidate characters after any sign, with their underscores in place.</param>
    /// <param name="negative">Whether the candidate carried a minus sign.</param>
    /// <param name="value">When the method returns <see langword="true" />, the resolved integer.</param>
    /// <returns>
    /// <see langword="true" /> when the body matches a YAML 1.1 integer form and fits in a 64-bit signed integer;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Every form may follow a sign, and groups its digits with underscores only after its first digit, so a body that
    /// begins with an underscore matches none of them. A leading zero makes the rest octal, so a token such as
    /// <c>083</c> or <c>09</c> matches no 1.1 form and remains a string rather than being read as a decimal; the 1.2
    /// core schema has no octal-by-leading-zero rule and reads such tokens as decimals.
    /// </remarks>
    private static bool TryResolveYaml11Integer(ReadOnlySpan<char> body, bool negative, out long value)
    {
        value = 0;

        if (body[0] is < '0' or > '9')
            return false;

        if (body.StartsWith("0b", StringComparison.Ordinal))
            return TryParseRadix(body[2..], 2, negative, grouped: true, out value);

        if (body.StartsWith("0x", StringComparison.Ordinal))
            return TryParseRadix(body[2..], 16, negative, grouped: true, out value);

        if (body.Length > 1 && body[0] == '0')
            return TryParseRadix(body, 8, negative, grouped: true, out value);

        return TryParseGroupedDecimal(body, negative, out value);
    }

    /// <summary>
    /// Attempts to parse a YAML 1.1 decimal integer whose digits may be grouped with underscores.
    /// </summary>
    /// <param name="digits">The digits after any sign, beginning with a digit.</param>
    /// <param name="negative">Whether the literal carried a minus sign.</param>
    /// <param name="value">When the method returns <see langword="true" />, the signed result.</param>
    /// <returns>
    /// <see langword="true" /> when the digits form a decimal integer that fits in a 64-bit signed integer; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryParseGroupedDecimal(ReadOnlySpan<char> digits, bool negative, out long value)
    {
        value = 0;

        // A decimal form has no leading zeros, so a run longer than the 19 digits of the 64-bit range cannot fit and
        // needs no parse; the buffer holds the sign and at most those 19 digits.
        Span<char> buffer = stackalloc char[20];
        int n = 0;
        if (negative)
            buffer[n++] = '-';

        foreach (char c in digits)
        {
            if (c == '_')
                continue;

            if (c is < '0' or > '9' || n == buffer.Length)
                return false;

            buffer[n++] = c;
        }

        return long.TryParse(buffer[..n], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Attempts to resolve a numeric candidate as a double-precision floating-point value.
    /// </summary>
    /// <param name="s">The candidate characters, transcoded by <see cref="TranscodeNumericCandidate" />.</param>
    /// <param name="value">When the method returns <see langword="true" />, the resolved value.</param>
    /// <returns>
    /// <see langword="true" /> when the scalar is a floating-point value; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryResolveFloat(ReadOnlySpan<char> s, out double value)
    {
        value = 0;

        if (s.Length == 0)
            return false;

        double sign = 1.0;
        ReadOnlySpan<char> body = s;
        if (s[0] is '+' or '-')
        {
            sign = s[0] == '-' ? -1.0 : 1.0;
            body = s[1..];
        }

        if (body is ".inf" or ".Inf" or ".INF")
        {
            value = sign * double.PositiveInfinity;
            return true;
        }

        if (s is ".nan" or ".NaN" or ".NAN")
        {
            value = double.NaN;
            return true;
        }

        // Require a digit and at least one float marker so plain integers are not misclassified as floats here.
        if (!HasFloatShape(body))
            return false;

        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Determines whether a candidate body has the shape of a YAML floating-point literal.
    /// </summary>
    /// <param name="body">The unsigned candidate text.</param>
    /// <returns>
    /// <see langword="true" /> when the text contains a decimal point or exponent and only float characters.
    /// </returns>
    private static bool HasFloatShape(ReadOnlySpan<char> body)
    {
        bool hasDigit = false;
        bool hasMarker = false;
        foreach (char c in body)
        {
            if (c is >= '0' and <= '9')
                hasDigit = true;
            else if (c is '.' or 'e' or 'E' or '+' or '-')
                hasMarker = true;
            else
                return false;
        }

        return hasDigit && hasMarker;
    }

    /// <summary>
    /// Parses an unsigned magnitude in the given radix and applies the sign.
    /// </summary>
    /// <param name="digits">The digits to parse, without prefix or sign.</param>
    /// <param name="radix">The numeric base, one of 2, 8, or 16.</param>
    /// <param name="negative">Whether the original literal carried a minus sign.</param>
    /// <param name="grouped">Whether underscores between the digits are YAML 1.1 digit-group separators.</param>
    /// <param name="value">When the method returns <see langword="true" />, the signed result.</param>
    /// <returns>
    /// <see langword="true" /> when the digits hold at least one digit and parse without overflow; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryParseRadix(ReadOnlySpan<char> digits, int radix, bool negative, bool grouped, out long value)
    {
        value = 0;

        bool hasDigit = false;
        ulong acc = 0;
        foreach (char c in digits)
        {
            if (grouped && c == '_')
                continue;

            int d = DigitValue(c);
            if (d < 0 || d >= radix)
                return false;

            // The bound is tested before multiplying: a wrapped product can land above the accumulator, so a test of
            // the result would let a literal beyond the 64-bit range resolve as the integer it wraps to.
            if (acc > (ulong.MaxValue - (ulong)d) / (ulong)radix)
                return false;

            acc = (acc * (ulong)radix) + (ulong)d;
            hasDigit = true;
        }

        if (!hasDigit)
            return false;

        if (negative)
        {
            if (acc > (ulong)long.MaxValue + 1)
                return false;

            value = unchecked(-(long)acc);
            return true;
        }

        if (acc > long.MaxValue)
            return false;

        value = (long)acc;
        return true;
    }

    /// <summary>
    /// Returns the numeric value of a hexadecimal digit, or <c>-1</c> when the character is not a digit.
    /// </summary>
    /// <param name="c">The character to evaluate.</param>
    /// <returns>The digit value in the range 0 to 15, or <c>-1</c>.</returns>
    private static int DigitValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };

    /// <summary>
    /// Determines whether every character of a candidate is a valid digit in the given radix.
    /// </summary>
    /// <param name="s">The characters to test.</param>
    /// <param name="radix">The numeric base.</param>
    /// <returns>
    /// <see langword="true" /> when all characters are digits in range; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsAllDigits(ReadOnlySpan<char> s, int radix)
    {
        foreach (char c in s)
        {
            int d = DigitValue(c);
            if (d < 0 || d >= radix)
                return false;
        }

        return s.Length > 0;
    }

    /// <summary>
    /// Determines whether a UTF-8 span equals the given ASCII token exactly.
    /// </summary>
    /// <param name="text">The UTF-8 bytes.</param>
    /// <param name="token">The ASCII token to compare against.</param>
    /// <returns><see langword="true" /> when the bytes match the token; otherwise <see langword="false" />.</returns>
    private static bool Matches(ReadOnlySpan<byte> text, string token)
    {
        if (text.Length != token.Length)
            return false;

        for (int i = 0; i < token.Length; i++)
        {
            if (text[i] != (byte)token[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether a UTF-8 span equals any of the given ASCII tokens exactly.
    /// </summary>
    /// <param name="text">The UTF-8 bytes.</param>
    /// <param name="tokens">The candidate ASCII tokens.</param>
    /// <returns>
    /// <see langword="true" /> when the bytes match one of the tokens; otherwise <see langword="false" />.
    /// </returns>
    private static bool MatchesAny(ReadOnlySpan<byte> text, params ReadOnlySpan<string> tokens)
    {
        foreach (string token in tokens)
        {
            if (Matches(text, token))
                return true;
        }

        return false;
    }
}
