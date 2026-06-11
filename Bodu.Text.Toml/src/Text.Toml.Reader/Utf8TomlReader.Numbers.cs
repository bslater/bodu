// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReader.Numbers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Toml.Reader;

public ref partial struct Utf8TomlReader
{
    /// <summary>
    /// Scans a boolean literal, caching its value.
    /// </summary>
    private void ScanBoolean()
    {
        if (TryConsumeKeyword("true"u8))
        {
            _boolValue = true;
        }
        else if (TryConsumeKeyword("false"u8))
        {
            _boolValue = false;
        }
        else
        {
            throw Error(TomlResourceStrings.Format_Invalid_TomlExpectedValue);
        }

        _tokenType = TomlTokenType.Boolean;
        SetValueSpan(_tokenStart, _pos - _tokenStart);
    }

    /// <summary>
    /// Consumes <paramref name="keyword" /> when it appears at the cursor followed by a value terminator.
    /// </summary>
    /// <param name="keyword">The keyword bytes to match.</param>
    /// <returns><see langword="true" /> when the keyword was consumed.</returns>
    private bool TryConsumeKeyword(ReadOnlySpan<byte> keyword)
    {
        if (_pos + keyword.Length > _source.Length)
            return false;
        if (!_source.Slice(_pos, keyword.Length).SequenceEqual(keyword))
            return false;

        var after = _pos + keyword.Length;
        if (after < _source.Length && !IsValueTerminator(_source[after]))
            return false;

        _pos = after;
        return true;
    }

    /// <summary>
    /// Scans a numeric value, or a date-time value when the cursor matches an RFC 3339 prefix.
    /// </summary>
    private void ScanNumberOrDateTime()
    {
        if (IsDigit(Peek(0)) && IsDigit(Peek(1)) && IsDigit(Peek(2)) && IsDigit(Peek(3)) && Peek(4) == (byte)'-')
        {
            ScanDateTime();
            return;
        }

        if (IsDigit(Peek(0)) && IsDigit(Peek(1)) && Peek(2) == (byte)':')
        {
            ScanLocalTime();
            return;
        }

        ScanNumber();
    }

    /// <summary>
    /// Scans an integer or float token, validating its grammar and caching the decoded value.
    /// </summary>
    private void ScanNumber()
    {
        var start = _pos;
        while (!Eof && !IsValueTerminator(Current))
            Advance();

        ReadOnlySpan<byte> token = _source[start.._pos];
        if (token.Length == 0)
            throw Error(TomlResourceStrings.Format_Invalid_TomlExpectedValue);

        SetValueSpan(start, token.Length);

        if (token.SequenceEqual("inf"u8) || token.SequenceEqual("+inf"u8))
        {
            SetFloat(double.PositiveInfinity);
            return;
        }

        if (token.SequenceEqual("-inf"u8))
        {
            SetFloat(double.NegativeInfinity);
            return;
        }

        if (token.SequenceEqual("nan"u8) || token.SequenceEqual("+nan"u8) || token.SequenceEqual("-nan"u8))
        {
            SetFloat(double.NaN);
            return;
        }

        if (token.Length >= 2 && token[0] == (byte)'0')
        {
            switch (token[1])
            {
                case (byte)'x':
                    SetInteger(ParseRadixInteger(token[2..], 16));
                    return;

                case (byte)'o':
                    SetInteger(ParseRadixInteger(token[2..], 8));
                    return;

                case (byte)'b':
                    SetInteger(ParseRadixInteger(token[2..], 2));
                    return;
            }
        }

        var isFloat = token.IndexOf((byte)'.') >= 0 || token.IndexOf((byte)'e') >= 0 || token.IndexOf((byte)'E') >= 0;
        if (isFloat)
            SetFloat(ParseFloat(token));
        else
            SetInteger(ParseDecimalInteger(token));
    }

    /// <summary>
    /// Records the current token as an <see cref="TomlTokenType.Integer" /> carrying <paramref name="value" />.
    /// </summary>
    /// <param name="value">The decoded integer value.</param>
    private void SetInteger(long value)
    {
        _longValue = value;
        _tokenType = TomlTokenType.Integer;
    }

    /// <summary>
    /// Records the current token as a <see cref="TomlTokenType.Float" /> carrying <paramref name="value" />.
    /// </summary>
    /// <param name="value">The decoded floating-point value.</param>
    private void SetFloat(double value)
    {
        _doubleValue = value;
        _tokenType = TomlTokenType.Float;
    }

    /// <summary>
    /// Parses a decimal integer token, enforcing underscore placement, the leading-zero rule, and the
    /// <see cref="long" /> range.
    /// </summary>
    /// <param name="token">The token bytes, including any sign.</param>
    /// <returns>The integer value.</returns>
    private readonly long ParseDecimalInteger(ReadOnlySpan<byte> token)
    {
        var i = 0;
        var negative = false;
        if (token[0] == (byte)'+' || token[0] == (byte)'-')
        {
            negative = token[0] == (byte)'-';
            i = 1;
        }

        ReadOnlySpan<byte> body = token[i..];
        if (body.IsEmpty)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidNumber);

        var digitCount = 0;
        byte firstDigit = 0;
        for (var j = 0; j < body.Length; j++)
        {
            var b = body[j];
            if (b == (byte)'_')
            {
                if (j == 0 || j == body.Length - 1 || !IsDigit(body[j - 1]) || !IsDigit(body[j + 1]))
                    throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidNumber);
                continue;
            }

            if (!IsDigit(b))
                throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidNumber);

            if (digitCount == 0)
                firstDigit = b;
            digitCount++;
        }

        if (digitCount == 0)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidNumber);
        if (digitCount > 1 && firstDigit == (byte)'0')
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidNumber);

        ulong magnitude = 0;
        foreach (var b in body)
        {
            if (b == (byte)'_')
                continue;

            var d = (uint)(b - (byte)'0');
            if (magnitude > (ulong.MaxValue - d) / 10)
                throw Error(TomlResourceStrings.Format_Invalid_TomlIntegerOutOfRange);
            magnitude = (magnitude * 10) + d;
        }

        if (negative)
        {
            if (magnitude > (ulong)long.MaxValue + 1)
                throw Error(TomlResourceStrings.Format_Invalid_TomlIntegerOutOfRange);
            return magnitude == (ulong)long.MaxValue + 1 ? long.MinValue : -(long)magnitude;
        }

        if (magnitude > long.MaxValue)
            throw Error(TomlResourceStrings.Format_Invalid_TomlIntegerOutOfRange);
        return (long)magnitude;
    }

    /// <summary>
    /// Parses a hexadecimal, octal, or binary integer body, enforcing underscore placement and the
    /// <see cref="long" /> range.
    /// </summary>
    /// <param name="body">The token bytes after the radix prefix.</param>
    /// <param name="radix">The numeric base (16, 8, or 2).</param>
    /// <returns>The integer value.</returns>
    private readonly long ParseRadixInteger(ReadOnlySpan<byte> body, int radix)
    {
        if (body.IsEmpty)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidNumber);

        var allowHexLetters = radix == 16;
        for (var j = 0; j < body.Length; j++)
        {
            if (body[j] != (byte)'_')
                continue;

            if (j == 0 || j == body.Length - 1 || !IsNeighborDigit(body[j - 1], allowHexLetters) || !IsNeighborDigit(body[j + 1], allowHexLetters))
                throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidNumber);
        }

        ulong value = 0;
        foreach (var b in body)
        {
            if (b == (byte)'_')
                continue;

            var d = HexValue(b);
            if (d < 0 || d >= radix)
                throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidNumber);
            if (value > (ulong.MaxValue - (ulong)d) / (ulong)radix)
                throw Error(TomlResourceStrings.Format_Invalid_TomlIntegerOutOfRange);
            value = (value * (ulong)radix) + (ulong)d;
        }

        if (value > long.MaxValue)
            throw Error(TomlResourceStrings.Format_Invalid_TomlIntegerOutOfRange);
        return (long)value;
    }

    /// <summary>
    /// Parses a floating-point token, enforcing underscore placement and the TOML float grammar.
    /// </summary>
    /// <param name="token">The token bytes.</param>
    /// <returns>The float value.</returns>
    private readonly double ParseFloat(ReadOnlySpan<byte> token)
    {
        Span<char> buffer = token.Length <= 128 ? stackalloc char[128] : new char[token.Length];
        var n = 0;
        for (var j = 0; j < token.Length; j++)
        {
            var b = token[j];
            if (b == (byte)'_')
            {
                if (j == 0 || j == token.Length - 1 || !IsDigit(token[j - 1]) || !IsDigit(token[j + 1]))
                    throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidNumber);
                continue;
            }

            buffer[n++] = (char)b;
        }

        ReadOnlySpan<char> clean = buffer[..n];
        if (!IsValidFloatLiteral(clean))
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidNumber);

        return double.Parse(clean, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Validates that <paramref name="s" /> is a TOML float literal (underscores already removed).
    /// </summary>
    /// <param name="s">The candidate literal.</param>
    /// <returns><see langword="true" /> when the literal is a valid TOML float.</returns>
    private static bool IsValidFloatLiteral(ReadOnlySpan<char> s)
    {
        var i = 0;
        if (i < s.Length && (s[i] == '+' || s[i] == '-'))
            i++;

        // Integer part: 0 or [1-9][0-9]*.
        var intStart = i;
        while (i < s.Length && s[i] is >= '0' and <= '9')
            i++;
        var intLen = i - intStart;
        if (intLen == 0)
            return false;
        if (intLen > 1 && s[intStart] == '0')
            return false;

        var hasFraction = false;
        if (i < s.Length && s[i] == '.')
        {
            hasFraction = true;
            i++;
            var fracStart = i;
            while (i < s.Length && s[i] is >= '0' and <= '9')
                i++;
            if (i == fracStart)
                return false;
        }

        var hasExponent = false;
        if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
        {
            hasExponent = true;
            i++;
            if (i < s.Length && (s[i] == '+' || s[i] == '-'))
                i++;
            var expStart = i;
            while (i < s.Length && s[i] is >= '0' and <= '9')
                i++;
            if (i == expStart)
                return false;
        }

        return i == s.Length && (hasFraction || hasExponent);
    }

    /// <summary>
    /// Indicates whether <paramref name="b" /> may sit adjacent to an underscore in a numeric literal of the given
    /// class.
    /// </summary>
    /// <param name="b">The byte.</param>
    /// <param name="allowHexLetters">When <see langword="true" />, hexadecimal letters are permitted.</param>
    /// <returns><see langword="true" /> when the byte is a permitted neighbor digit.</returns>
    private static bool IsNeighborDigit(byte b, bool allowHexLetters) =>
        allowHexLetters ? HexValue(b) >= 0 : IsDigit(b);
}
