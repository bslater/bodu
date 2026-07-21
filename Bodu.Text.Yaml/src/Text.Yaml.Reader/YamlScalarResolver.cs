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

        // The integer and float attempts share one transcode (with 1.1 underscores stripped): a scalar carrying a
        // non-ASCII byte cannot be numeric, so resolution short-circuits to a string without allocating.
        Span<char> buffer = text.Length <= StackallocCharThreshold ? stackalloc char[StackallocCharThreshold] : new char[text.Length];
        int length = TranscodeNumericCandidate(text, version, buffer);
        if (length < 0)
            return YamlValueKind.String;

        ReadOnlySpan<char> candidate = buffer[..length];

        if (TryResolveInteger(candidate, version, out long longValue))
        {
            bits = longValue;
            return YamlValueKind.Integer;
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
        text.Length == 1 && text[0] == (byte)'~'
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
    /// Transcodes a scalar's bytes into the numeric-candidate characters both numeric attempts parse, removing the 1.1
    /// schema's underscore digit-group separators.
    /// </summary>
    /// <param name="text">The raw UTF-8 bytes of the scalar.</param>
    /// <param name="version">The specification version, which controls underscore stripping.</param>
    /// <param name="destination">The buffer that receives the characters; at least the scalar's length.</param>
    /// <returns>
    /// The number of characters written, or <c>-1</c> when a non-ASCII byte makes the scalar non-numeric.
    /// </returns>
    private static int TranscodeNumericCandidate(ReadOnlySpan<byte> text, YamlSpecVersion version, Span<char> destination)
    {
        bool stripUnderscores = version == YamlSpecVersion.V1_1;
        int n = 0;
        foreach (byte b in text)
        {
            if (b >= 0x80)
                return -1;

            if (stripUnderscores && b == (byte)'_')
                continue;

            destination[n++] = (char)b;
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

        // Non-decimal radix forms carry no sign in either schema; a signed radix literal is not an integer and falls
        // through to remain a string.
        if (!hasSign && body.StartsWith("0x", StringComparison.Ordinal))
            return TryParseRadix(body[2..], 16, negative, out value);

        if (!hasSign && version == YamlSpecVersion.V1_1 && body.StartsWith("0b", StringComparison.Ordinal))
            return TryParseRadix(body[2..], 2, negative, out value);

        if (!hasSign && body.StartsWith("0o", StringComparison.Ordinal))
            return TryParseRadix(body[2..], 8, negative, out value);

        // YAML 1.1 octal uses a leading zero (0NNN); the 1.2 core schema does not.
        if (!hasSign && version == YamlSpecVersion.V1_1 && body.Length > 1 && body[0] == '0' && IsAllDigits(body, 8))
            return TryParseRadix(body[1..], 8, negative, out value);

        if (!IsAllDigits(body, 10))
            return false;

        return long.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
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
    /// <param name="value">When the method returns <see langword="true" />, the signed result.</param>
    /// <returns>
    /// <see langword="true" /> when the digits parse without overflow; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryParseRadix(ReadOnlySpan<char> digits, int radix, bool negative, out long value)
    {
        value = 0;
        if (digits.Length == 0)
            return false;

        ulong acc = 0;
        foreach (char c in digits)
        {
            int d = DigitValue(c);
            if (d < 0 || d >= radix)
                return false;

            ulong next = acc * (ulong)radix + (ulong)d;
            if (next < acc)
                return false;

            acc = next;
        }

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
