// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReader.Strings.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Toml.Reader;

public ref partial struct Utf8TomlReader
{
    /// <summary>
    /// Scans a single-line basic string (its opening quote at the cursor), validating escapes, control characters, and
    /// UTF-8 without materializing the value.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the string was scanned; <see langword="false" /> when more data is required.
    /// </returns>
    private bool TryScanBasicString()
    {
        Advance();

        int start = _pos;
        bool escapes = false;
        while (true)
        {
            if (Eof)
            {
                if (!_isFinalBlock)
                    return NeedMoreData();
                throw Error(TomlResourceStrings.Format_Invalid_TomlUnterminatedString);
            }

            if (PendingCarriageReturn())
                return NeedMoreData();

            if (AtNewline())
                throw Error(TomlResourceStrings.Format_Invalid_TomlUnterminatedString);

            byte b = Current;
            if (b == (byte)'"')
            {
                SetValueSpan(start, _pos - start);
                Advance();
                break;
            }

            if (b == (byte)'\\')
            {
                escapes = true;
                Advance();
                if (!TryValidateEscape())
                    return false;
                continue;
            }

            if (b == 0x7F || (b < 0x20 && b != 0x09))
                throw Error(TomlResourceStrings.Format_Invalid_TomlControlCharacter);

            if (b < 0x80)
            {
                Advance();
            }
            else if (!TryConsumeUtf8Sequence())
            {
                return false;
            }
        }

        _textKind = TomlScalarTextKind.Basic;
        _hasEscapes = escapes;
        return true;
    }

    /// <summary>
    /// Scans a single-line literal string (its opening apostrophe at the cursor).
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the string was scanned; <see langword="false" /> when more data is required.
    /// </returns>
    private bool TryScanLiteralString()
    {
        Advance();

        int start = _pos;
        while (true)
        {
            if (Eof)
            {
                if (!_isFinalBlock)
                    return NeedMoreData();
                throw Error(TomlResourceStrings.Format_Invalid_TomlUnterminatedString);
            }

            if (PendingCarriageReturn())
                return NeedMoreData();

            if (AtNewline())
                throw Error(TomlResourceStrings.Format_Invalid_TomlUnterminatedString);

            byte b = Current;
            if (b == (byte)'\'')
            {
                SetValueSpan(start, _pos - start);
                Advance();
                break;
            }

            if (b == 0x7F || (b < 0x20 && b != 0x09))
                throw Error(TomlResourceStrings.Format_Invalid_TomlControlCharacter);

            if (b < 0x80)
            {
                Advance();
            }
            else if (!TryConsumeUtf8Sequence())
            {
                return false;
            }
        }

        _textKind = TomlScalarTextKind.Verbatim;
        _hasEscapes = false;
        return true;
    }

    /// <summary>
    /// Scans a multi-line basic string (its opening triple quote at the cursor). The content range starts after the
    /// trimmed leading newline; raw newline bytes inside the content are preserved as written.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the string was scanned; <see langword="false" /> when more data is required.
    /// </returns>
    private bool TryScanMultilineBasicString()
    {
        _pos += 3;
        if (PendingCarriageReturn())
            return NeedMoreData();
        if (AtNewline())
            ConsumeNewline();

        int start = _pos;
        bool escapes = false;
        while (true)
        {
            if (Eof)
            {
                if (!_isFinalBlock)
                    return NeedMoreData();
                throw Error(TomlResourceStrings.Format_Invalid_TomlUnterminatedString);
            }

            byte b = Current;
            if (b == (byte)'"')
            {
                int run = CountRun((byte)'"');

                // A quote run touching the end of a non-final buffer may still grow.
                if (!_isFinalBlock && _pos + run >= _source.Length)
                    return NeedMoreData();

                if (run >= 3)
                {
                    if (run - 3 > 2)
                        throw Error(TomlResourceStrings.Format_Invalid_TomlUnterminatedString);

                    // Quotes beyond the closing delimiter belong to the content and sit at the start of the run.
                    SetValueSpan(start, (_pos + (run - 3)) - start);
                    _pos += run;
                    break;
                }

                _pos += run;
                continue;
            }

            if (b == (byte)'\\')
            {
                escapes = true;
                if (IsLineEndingBackslash())
                {
                    ConsumeLineEndingBackslash();
                    continue;
                }

                // Whitespace after the backslash that reaches the buffer end may become a line-ending backslash.
                if (!_isFinalBlock && IsPotentialLineEndingBackslash())
                    return NeedMoreData();

                Advance();
                if (!TryValidateEscape())
                    return false;
                continue;
            }

            if (PendingCarriageReturn())
                return NeedMoreData();

            if (AtNewline())
            {
                ConsumeNewline();
                continue;
            }

            if (b == 0x7F || (b < 0x20 && b != 0x09))
                throw Error(TomlResourceStrings.Format_Invalid_TomlControlCharacter);

            if (b < 0x80)
            {
                Advance();
            }
            else if (!TryConsumeUtf8Sequence())
            {
                return false;
            }
        }

        _textKind = TomlScalarTextKind.MultilineBasic;
        _hasEscapes = escapes;
        return true;
    }

    /// <summary>
    /// Scans a multi-line literal string (its opening triple apostrophe at the cursor). The content range starts after
    /// the trimmed leading newline, so decoding is a direct transcode.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the string was scanned; <see langword="false" /> when more data is required.
    /// </returns>
    private bool TryScanMultilineLiteralString()
    {
        _pos += 3;
        if (PendingCarriageReturn())
            return NeedMoreData();
        if (AtNewline())
            ConsumeNewline();

        int start = _pos;
        while (true)
        {
            if (Eof)
            {
                if (!_isFinalBlock)
                    return NeedMoreData();
                throw Error(TomlResourceStrings.Format_Invalid_TomlUnterminatedString);
            }

            byte b = Current;
            if (b == (byte)'\'')
            {
                int run = CountRun((byte)'\'');

                // An apostrophe run touching the end of a non-final buffer may still grow.
                if (!_isFinalBlock && _pos + run >= _source.Length)
                    return NeedMoreData();

                if (run >= 3)
                {
                    if (run - 3 > 2)
                        throw Error(TomlResourceStrings.Format_Invalid_TomlUnterminatedString);

                    // Apostrophes beyond the closing delimiter belong to the content and sit at the start of the run.
                    SetValueSpan(start, (_pos + (run - 3)) - start);
                    _pos += run;
                    break;
                }

                _pos += run;
                continue;
            }

            if (PendingCarriageReturn())
                return NeedMoreData();

            if (AtNewline())
            {
                ConsumeNewline();
                continue;
            }

            if (b == 0x7F || (b < 0x20 && b != 0x09))
                throw Error(TomlResourceStrings.Format_Invalid_TomlControlCharacter);

            if (b < 0x80)
            {
                Advance();
            }
            else if (!TryConsumeUtf8Sequence())
            {
                return false;
            }
        }

        _textKind = TomlScalarTextKind.Verbatim;
        _hasEscapes = false;
        return true;
    }

    /// <summary>
    /// Validates and consumes the escape sequence beginning at the cursor (which is positioned just after the
    /// backslash).
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the escape is valid; <see langword="false" /> when more data is required.
    /// </returns>
    private bool TryValidateEscape()
    {
        if (Eof)
        {
            if (!_isFinalBlock)
                return NeedMoreData();
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidEscape);
        }

        byte e = Current;

        // The \e (escape) and \xHH (hex byte) escapes were introduced in TOML v1.1.0; reject them under v1.0.
        if (_specVersion != TomlSpecVersion.V1_1 && (e == (byte)'e' || e == (byte)'x'))
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidEscape);

        switch (e)
        {
            case (byte)'"':
            case (byte)'\\':
            case (byte)'b':
            case (byte)'t':
            case (byte)'n':
            case (byte)'f':
            case (byte)'r':
            case (byte)'e':
                Advance();
                return true;

            case (byte)'x':
                Advance();
                return TryValidateUnicodeEscape(2);

            case (byte)'u':
                Advance();
                return TryValidateUnicodeEscape(4);

            case (byte)'U':
                Advance();
                return TryValidateUnicodeEscape(8);

            default:
                throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidEscape);
        }
    }

    /// <summary>
    /// Validates and consumes <paramref name="digits" /> hexadecimal digits, rejecting values that are not Unicode
    /// scalar values.
    /// </summary>
    /// <param name="digits">The number of hexadecimal digits to read.</param>
    /// <returns>
    /// <see langword="true" /> when the escape is valid; <see langword="false" /> when more data is required.
    /// </returns>
    private bool TryValidateUnicodeEscape(int digits)
    {
        if (_pos + digits > _source.Length)
        {
            if (!_isFinalBlock)
                return NeedMoreData();
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidEscape);
        }

        long value = 0;
        for (int i = 0; i < digits; i++)
        {
            int d = HexValue(_source[_pos + i]);
            if (d < 0)
                throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidEscape);
            value = (value << 4) | (uint)d;
        }

        _pos += digits;

        if (value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF))
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidEscape);

        return true;
    }

    /// <summary>
    /// Indicates whether the backslash at the cursor, followed only by whitespace up to the end of the buffer (or by
    /// whitespace and a trailing carriage return), might become a line-ending backslash once more data arrives.
    /// </summary>
    /// <returns><see langword="true" /> when the classification needs more data.</returns>
    private readonly bool IsPotentialLineEndingBackslash()
    {
        int j = _pos + 1;
        while (j < _source.Length && (_source[j] == (byte)' ' || _source[j] == (byte)'\t'))
            j++;
        if (j >= _source.Length)
            return true;
        return _source[j] == (byte)'\r' && j + 1 >= _source.Length;
    }

    /// <summary>
    /// Indicates whether the backslash at the cursor is a line-ending backslash (escape, optional whitespace, then a
    /// newline).
    /// </summary>
    /// <returns><see langword="true" /> when a line-ending backslash begins at the cursor.</returns>
    private readonly bool IsLineEndingBackslash()
    {
        int j = _pos + 1;
        while (j < _source.Length && (_source[j] == (byte)' ' || _source[j] == (byte)'\t'))
            j++;
        return j < _source.Length && (_source[j] == (byte)'\n' || (_source[j] == (byte)'\r' && j + 1 < _source.Length && _source[j + 1] == (byte)'\n'));
    }

    /// <summary>
    /// Consumes a line-ending backslash and all following whitespace and newlines.
    /// </summary>
    private void ConsumeLineEndingBackslash()
    {
        Advance();
        while (!Eof && (Current == (byte)' ' || Current == (byte)'\t'))
            Advance();
        ConsumeNewline();
        while (!Eof && (Current == (byte)' ' || Current == (byte)'\t' || AtNewline()))
        {
            if (AtNewline())
                ConsumeNewline();
            else
                Advance();
        }
    }

    /// <summary>
    /// Decodes the validated content of a basic string, resolving escape sequences and — for the multi-line form —
    /// line-ending backslashes.
    /// </summary>
    /// <param name="content">The raw content bytes, already validated by the scan.</param>
    /// <returns>The decoded string.</returns>
    internal static string DecodeEscapedString(ReadOnlySpan<byte> content)
    {
        var sb = new StringBuilder(content.Length);
        int i = 0;
        while (i < content.Length)
        {
            if (content[i] != (byte)'\\')
            {
                // Copy the verbatim run up to the next escape in one transcode.
                var run = content[i..];
                int next = run.IndexOf((byte)'\\');
                if (next < 0)
                    next = run.Length;

                sb.Append(Encoding.UTF8.GetString(run[..next]));
                i += next;
                continue;
            }

            i++;
            byte e = content[i];
            switch (e)
            {
                case (byte)'"': sb.Append('"'); i++; break;
                case (byte)'\\': sb.Append('\\'); i++; break;
                case (byte)'b': sb.Append('\b'); i++; break;
                case (byte)'t': sb.Append('\t'); i++; break;
                case (byte)'n': sb.Append('\n'); i++; break;
                case (byte)'f': sb.Append('\f'); i++; break;
                case (byte)'r': sb.Append('\r'); i++; break;
                case (byte)'e': sb.Append('\u001b'); i++; break;

                case (byte)'x': i++; AppendUnicodeScalar(sb, content, ref i, 2); break;
                case (byte)'u': i++; AppendUnicodeScalar(sb, content, ref i, 4); break;
                case (byte)'U': i++; AppendUnicodeScalar(sb, content, ref i, 8); break;

                default:
                    // The scan admits a backslash followed by whitespace only as a line-ending backslash: skip the
                    // whitespace, the newline, and all following whitespace and newlines.
                    while (i < content.Length && (content[i] == (byte)' ' || content[i] == (byte)'\t' || content[i] == (byte)'\r' || content[i] == (byte)'\n'))
                        i++;
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Decodes the validated content of a string token, transcoding the raw bytes directly when escape-free and
    /// resolving escapes otherwise.
    /// </summary>
    /// <param name="content">The raw content bytes, already validated by the scan.</param>
    /// <param name="hasEscapes">
    /// <see langword="true" /> when the content carries escape sequences or line-ending backslashes that must be
    /// resolved; otherwise <see langword="false" />.
    /// </param>
    /// <returns>The decoded string.</returns>
    internal static string DecodeString(ReadOnlySpan<byte> content, bool hasEscapes) =>
        hasEscapes ? DecodeEscapedString(content) : Encoding.UTF8.GetString(content);

    /// <summary>
    /// Appends the Unicode scalar value spelled by <paramref name="digits" /> validated hexadecimal digits.
    /// </summary>
    /// <param name="sb">The destination builder.</param>
    /// <param name="content">The content bytes.</param>
    /// <param name="i">The index of the first hex digit; advanced past the digits on return.</param>
    /// <param name="digits">The number of hexadecimal digits to read.</param>
    private static void AppendUnicodeScalar(StringBuilder sb, ReadOnlySpan<byte> content, ref int i, int digits)
    {
        int value = 0;
        for (int d = 0; d < digits; d++)
            value = (value << 4) | HexValue(content[i + d]);

        i += digits;
        sb.Append(char.ConvertFromUtf32(value));
    }
}
