// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReader.TryGet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Toml.Reader;

public ref partial struct Utf8TomlReader
{
    /// <summary>
    /// Reads the current token as an 8-bit unsigned integer.
    /// </summary>
    /// <returns>The integer value, narrowed from TOML's 64-bit integer.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the value does not fit in a <see cref="byte" />.</exception>
    public readonly byte GetByte() =>
        TryGetByte(out byte value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to read the current token as an 8-bit unsigned integer.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the narrowed value; otherwise zero.
    /// </param>
    /// <returns><see langword="true" /> when the value fits in a <see cref="byte" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    public readonly bool TryGetByte(out byte value)
    {
        long raw = GetInt64();
        if (raw is < byte.MinValue or > byte.MaxValue)
        {
            value = 0;
            return false;
        }

        value = (byte)raw;
        return true;
    }

    /// <summary>
    /// Reads the current token as an 8-bit signed integer.
    /// </summary>
    /// <returns>The integer value, narrowed from TOML's 64-bit integer.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the value does not fit in an <see cref="sbyte" />.</exception>
    public readonly sbyte GetSByte() =>
        TryGetSByte(out sbyte value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to read the current token as an 8-bit signed integer.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the narrowed value; otherwise zero.
    /// </param>
    /// <returns><see langword="true" /> when the value fits in an <see cref="sbyte" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    public readonly bool TryGetSByte(out sbyte value)
    {
        long raw = GetInt64();
        if (raw is < sbyte.MinValue or > sbyte.MaxValue)
        {
            value = 0;
            return false;
        }

        value = (sbyte)raw;
        return true;
    }

    /// <summary>
    /// Reads the current token as a 16-bit signed integer.
    /// </summary>
    /// <returns>The integer value, narrowed from TOML's 64-bit integer.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the value does not fit in a <see cref="short" />.</exception>
    public readonly short GetInt16() =>
        TryGetInt16(out short value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to read the current token as a 16-bit signed integer.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the narrowed value; otherwise zero.
    /// </param>
    /// <returns><see langword="true" /> when the value fits in a <see cref="short" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    public readonly bool TryGetInt16(out short value)
    {
        long raw = GetInt64();
        if (raw is < short.MinValue or > short.MaxValue)
        {
            value = 0;
            return false;
        }

        value = (short)raw;
        return true;
    }

    /// <summary>
    /// Reads the current token as a 32-bit signed integer.
    /// </summary>
    /// <returns>The integer value, narrowed from TOML's 64-bit integer.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the value does not fit in an <see cref="int" />.</exception>
    public readonly int GetInt32() =>
        TryGetInt32(out int value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to read the current token as a 32-bit signed integer.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the narrowed value; otherwise zero.
    /// </param>
    /// <returns><see langword="true" /> when the value fits in an <see cref="int" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    public readonly bool TryGetInt32(out int value)
    {
        long raw = GetInt64();
        if (raw is < int.MinValue or > int.MaxValue)
        {
            value = 0;
            return false;
        }

        value = (int)raw;
        return true;
    }

    /// <summary>
    /// Reads the current token as a 16-bit unsigned integer.
    /// </summary>
    /// <returns>The integer value, narrowed from TOML's 64-bit integer.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the value does not fit in a <see cref="ushort" />.</exception>
    public readonly ushort GetUInt16() =>
        TryGetUInt16(out ushort value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to read the current token as a 16-bit unsigned integer.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the narrowed value; otherwise zero.
    /// </param>
    /// <returns><see langword="true" /> when the value fits in a <see cref="ushort" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    public readonly bool TryGetUInt16(out ushort value)
    {
        long raw = GetInt64();
        if (raw is < ushort.MinValue or > ushort.MaxValue)
        {
            value = 0;
            return false;
        }

        value = (ushort)raw;
        return true;
    }

    /// <summary>
    /// Reads the current token as a 32-bit unsigned integer.
    /// </summary>
    /// <returns>The integer value, narrowed from TOML's 64-bit integer.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the value does not fit in a <see cref="uint" />.</exception>
    public readonly uint GetUInt32() =>
        TryGetUInt32(out uint value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to read the current token as a 32-bit unsigned integer.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the narrowed value; otherwise zero.
    /// </param>
    /// <returns><see langword="true" /> when the value fits in a <see cref="uint" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    public readonly bool TryGetUInt32(out uint value)
    {
        long raw = GetInt64();
        if (raw is < uint.MinValue or > uint.MaxValue)
        {
            value = 0;
            return false;
        }

        value = (uint)raw;
        return true;
    }

    /// <summary>
    /// Reads the current token as a 64-bit unsigned integer.
    /// </summary>
    /// <returns>The integer value, converted from TOML's 64-bit signed integer.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the value is negative.</exception>
    public readonly ulong GetUInt64() =>
        TryGetUInt64(out ulong value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to read the current token as a 64-bit unsigned integer.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the converted value; otherwise zero.
    /// </param>
    /// <returns><see langword="true" /> when the value is non-negative.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    /// <remarks>
    /// TOML integers are 64-bit signed, so a value above <see cref="long.MaxValue" /> cannot appear in a document; only
    /// a negative value fails the conversion.
    /// </remarks>
    public readonly bool TryGetUInt64(out ulong value)
    {
        long raw = GetInt64();
        if (raw < 0)
        {
            value = 0;
            return false;
        }

        value = (ulong)raw;
        return true;
    }

    /// <summary>
    /// Reads the current token as an IEEE 754 binary32 floating-point value, parsed from the raw float literal.
    /// </summary>
    /// <returns>The single-precision value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Float" />.
    /// </exception>
    public readonly float GetSingle() =>
        TryGetSingle(out float value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to read the current token as an IEEE 754 binary32 floating-point value.
    /// </summary>
    /// <param name="value">When this method returns <see langword="true" />, the single-precision value.</param>
    /// <returns>
    /// <see langword="true" /> always; the conversion cannot fail. A magnitude beyond the <see cref="float" /> range
    /// rounds to an infinity.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Float" />.
    /// </exception>
    public readonly bool TryGetSingle(out float value)
    {
        double raw = GetDouble();
        if (double.IsNaN(raw) || double.IsInfinity(raw))
        {
            value = (float)raw;
            return true;
        }

        value = float.Parse(StripNumberUnderscores(ValueSpan), NumberStyles.Float, CultureInfo.InvariantCulture);
        return true;
    }

    /// <summary>
    /// Reads the current token as a decimal, parsed exactly from the raw float literal.
    /// </summary>
    /// <returns>The decimal value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Float" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the literal is <c>inf</c>, <c>-inf</c>, or <c>nan</c>, or its magnitude exceeds the
    /// <see cref="decimal" /> range.
    /// </exception>
    public readonly decimal GetDecimal() =>
        TryGetDecimal(out decimal value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to read the current token as a decimal, parsed exactly from the raw float literal.
    /// </summary>
    /// <param name="value">When this method returns <see langword="true" />, the decimal value; otherwise zero.</param>
    /// <returns>
    /// <see langword="true" /> when the literal is finite and within the <see cref="decimal" /> range.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Float" />.
    /// </exception>
    /// <remarks>
    /// The decimal is parsed from the document's raw literal rather than converted from the binary64 value, so digits
    /// that binary64 cannot represent exactly are preserved.
    /// </remarks>
    public readonly bool TryGetDecimal(out decimal value)
    {
        double raw = GetDouble();
        if (double.IsNaN(raw) || double.IsInfinity(raw))
        {
            value = 0;
            return false;
        }

        return decimal.TryParse(StripNumberUnderscores(ValueSpan), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Reads the current token as a <see cref="Guid" />, parsed from a string token in the 36-character hyphenated (
    /// <c>D</c>) format.
    /// </summary>
    /// <returns>The parsed GUID.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.String" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the string is not a GUID in the <c>D</c> format.</exception>
    public readonly Guid GetGuid() =>
        TryGetGuid(out var value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to read the current token as a <see cref="Guid" /> in the 36-character hyphenated (<c>D</c>) format.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the parsed GUID; otherwise <see cref="Guid.Empty" />.
    /// </param>
    /// <returns><see langword="true" /> when the string parses as a GUID in the <c>D</c> format.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.String" />.
    /// </exception>
    public readonly bool TryGetGuid(out Guid value)
    {
        if (_tokenType != TomlTokenType.String)
            throw new InvalidOperationException();

        return Guid.TryParseExact(GetString(), "D", out value);
    }

    /// <summary>
    /// Removes the underscores from a numeric literal, returning the characters to parse.
    /// </summary>
    /// <param name="literal">The raw literal bytes, already validated by the scan.</param>
    /// <returns>The literal characters with underscores removed.</returns>
    private static ReadOnlySpan<char> StripNumberUnderscores(ReadOnlySpan<byte> literal)
    {
        char[] buffer = new char[literal.Length];
        int n = 0;
        foreach (byte b in literal)
        {
            if (b != (byte)'_')
                buffer[n++] = (char)b;
        }

        return buffer.AsSpan(0, n);
    }
}
