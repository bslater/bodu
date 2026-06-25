// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlParser.Scalars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Scalar parsing for the YAML parser: plain scalars (with multi-line folding and implicit type resolution), single-
/// and double-quoted scalars (with escapes and folding), and literal and folded block scalars with chomping.
/// </summary>
internal sealed partial class YamlParser
{
    /// <summary>
    /// Parses a scalar node in block context, dispatching to quoted or plain parsing, and resolves the implicit type of
    /// single-line plain scalars.
    /// </summary>
    /// <param name="minIndent">The minimum continuation indentation for a multi-line plain scalar.</param>
    /// <returns>The row index of the scalar.</returns>
    private int ParseBlockScalarPlainOrQuoted(int minIndent)
    {
        var offset = _pos;
        var c = Peek();

        if (c == (byte)'"')
            return NewString(ReadDoubleQuoted(), YamlScalarStyle.DoubleQuoted, offset, null, null);

        if (c == (byte)'\'')
            return NewString(ReadSingleQuoted(), YamlScalarStyle.SingleQuoted, offset, null, null);

        return ReadPlainBlock(minIndent, offset);
    }

    /// <summary>
    /// Reads a plain scalar in block context, folding continuation lines and resolving its implicit type.
    /// </summary>
    /// <param name="minIndent">The minimum continuation indentation.</param>
    /// <param name="offset">The byte offset where the scalar begins.</param>
    /// <returns>The row index of the scalar.</returns>
    private int ReadPlainBlock(int minIndent, int offset)
    {
        var firstStart = _pos;
        var firstEnd = ScanPlainLineEnd();

        var folded = false;
        StringBuilder? sb = null;

        while (true)
        {
            var savePos = _pos;
            var saveLine = _line;
            var saveLineStart = _lineStart;

            if (AtEnd)
                break;

            Advance(); // consume the line break ending the previous content line

            var blanks = 0;
            while (true)
            {
                var lineStart = _pos;
                var p = _pos;
                while (p < _length && _source[p] is (byte)' ' or (byte)'\t')
                    p++;

                var blank = p >= _length || _source[p] is (byte)'\n' or (byte)'\r';
                if (blank)
                {
                    if (p >= _length)
                    {
                        _pos = p;
                        break;
                    }

                    _pos = p;
                    Advance();
                    blanks++;
                    continue;
                }

                var col = p - lineStart;
                _pos = p;

                if (col < minIndent || AtDocumentBoundary() || _source[p] == (byte)'#')
                {
                    _pos = savePos;
                    _line = saveLine;
                    _lineStart = saveLineStart;
                    goto done;
                }

                // A continuation line that is itself a structural indicator ends the scalar.
                break;
            }

            if (AtEnd)
            {
                _pos = savePos;
                _line = saveLine;
                _lineStart = saveLineStart;
                break;
            }

            sb ??= new StringBuilder().Append(Utf8(firstStart, firstEnd - firstStart));
            folded = true;
            sb.Append(blanks == 0 ? " " : new string('\n', blanks));

            var contStart = _pos;
            var contEnd = ScanPlainLineEnd();
            sb.Append(Utf8(contStart, contEnd - contStart));
        }

    done:
        if (!folded)
        {
            ReadOnlySpan<byte> span = _source.AsSpan(firstStart, firstEnd - firstStart);
            var kind = YamlScalarResolver.Resolve(span, _version, out var bits);
            if (kind != YamlValueKind.String)
                return NewScalar(kind, bits, YamlScalarStyle.Plain, offset, null, null);

            return NewString(Utf8(firstStart, firstEnd - firstStart), YamlScalarStyle.Plain, offset, null, null);
        }

        return NewString(sb!.ToString(), YamlScalarStyle.Plain, offset, null, null);
    }

    /// <summary>
    /// Advances the cursor to the end of the current plain-scalar line (before the line break), stopping at an inline
    /// comment, and returns the byte offset just past the last non-space content byte.
    /// </summary>
    /// <returns>The byte offset of the end of the trimmed content.</returns>
    private int ScanPlainLineEnd()
    {
        var end = _pos;
        while (!IsBreakOrEnd(Peek()))
        {
            if (Peek() == (byte)' ' && PeekAt(1) == (byte)'#')
                break;

            // A ' : ' value indicator does not occur here (mapping detection runs first), but a trailing ':' at
            // end of line is preserved as content.
            _pos++;
            if (_source[_pos - 1] is not ((byte)' ' or (byte)'\t'))
                end = _pos;
        }

        // If we stopped at an inline comment, advance to the line break so the caller can fold correctly.
        while (!IsBreakOrEnd(Peek()))
            _pos++;

        return end;
    }

    /// <summary>
    /// Reads a single-quoted scalar, decoding doubled quotes and folding line breaks.
    /// </summary>
    /// <returns>The decoded string.</returns>
    /// <exception cref="YamlFormatException">The scalar is not terminated.</exception>
    private string ReadSingleQuoted()
    {
        Advance(); // opening quote
        var buf = new List<byte>();
        var pendingBreaks = 0;

        while (true)
        {
            if (AtEnd)
                throw Error(YamlResourceStrings.Format_Invalid_YamlUnterminatedScalar);

            var b = Peek();
            if (b == (byte)'\'')
            {
                if (PeekAt(1) == (byte)'\'')
                {
                    FlushFold(buf, ref pendingBreaks);
                    buf.Add((byte)'\'');
                    Advance();
                    Advance();
                    continue;
                }

                Advance();
                break;
            }

            if (b is (byte)'\n' or (byte)'\r')
            {
                TrimTrailingInlineSpace(buf);
                Advance();
                pendingBreaks++;
                SkipSpaces();
                continue;
            }

            FlushFold(buf, ref pendingBreaks);
            buf.Add(b);
            _pos++;
        }

        return Encoding.UTF8.GetString(buf.ToArray());
    }

    /// <summary>
    /// Reads a double-quoted scalar, decoding backslash escapes and folding line breaks.
    /// </summary>
    /// <returns>The decoded string.</returns>
    /// <exception cref="YamlFormatException">The scalar is not terminated or contains an invalid escape.</exception>
    private string ReadDoubleQuoted()
    {
        Advance(); // opening quote
        var buf = new List<byte>();
        var pendingBreaks = 0;

        while (true)
        {
            if (AtEnd)
                throw Error(YamlResourceStrings.Format_Invalid_YamlUnterminatedScalar);

            var b = Peek();
            if (b == (byte)'"')
            {
                Advance();
                break;
            }

            if (b == (byte)'\\')
            {
                var next = PeekAt(1);
                if (next is (byte)'\n' or (byte)'\r')
                {
                    // Escaped line break: line continuation with no folded space.
                    Advance();
                    Advance();
                    SkipSpaces();
                    pendingBreaks = 0;
                    continue;
                }

                FlushFold(buf, ref pendingBreaks);
                Advance();
                ReadEscape(buf);
                continue;
            }

            if (b is (byte)'\n' or (byte)'\r')
            {
                TrimTrailingInlineSpace(buf);
                Advance();
                pendingBreaks++;
                SkipSpaces();
                continue;
            }

            FlushFold(buf, ref pendingBreaks);
            buf.Add(b);
            _pos++;
        }

        return Encoding.UTF8.GetString(buf.ToArray());
    }

    /// <summary>
    /// Decodes a single backslash escape sequence following the backslash and appends it to the buffer.
    /// </summary>
    /// <param name="buf">The output byte buffer.</param>
    /// <exception cref="YamlFormatException">The escape sequence is not valid.</exception>
    private void ReadEscape(List<byte> buf)
    {
        var e = Peek();
        Advance();
        switch (e)
        {
            case (byte)'0': buf.Add(0); break;
            case (byte)'a': buf.Add(0x07); break;
            case (byte)'b': buf.Add(0x08); break;
            case (byte)'t': buf.Add((byte)'\t'); break;
            case (byte)'n': buf.Add((byte)'\n'); break;
            case (byte)'v': buf.Add(0x0B); break;
            case (byte)'f': buf.Add(0x0C); break;
            case (byte)'r': buf.Add((byte)'\r'); break;
            case (byte)'e': buf.Add(0x1B); break;
            case (byte)' ': buf.Add((byte)' '); break;
            case (byte)'"': buf.Add((byte)'"'); break;
            case (byte)'/': buf.Add((byte)'/'); break;
            case (byte)'\\': buf.Add((byte)'\\'); break;
            case (byte)'N': AppendRune(buf, 0x85); break;
            case (byte)'_': AppendRune(buf, 0xA0); break;
            case (byte)'L': AppendRune(buf, 0x2028); break;
            case (byte)'P': AppendRune(buf, 0x2029); break;
            case (byte)'x': AppendRune(buf, ReadHex(2)); break;
            case (byte)'u': AppendRune(buf, ReadHex(4)); break;
            case (byte)'U': AppendRune(buf, ReadHex(8)); break;
            default: throw Error(YamlResourceStrings.Format_Invalid_YamlInvalidEscape);
        }
    }

    /// <summary>
    /// Reads a fixed number of hexadecimal digits as a code point.
    /// </summary>
    /// <param name="count">The number of hexadecimal digits to read.</param>
    /// <returns>The parsed code point.</returns>
    /// <exception cref="YamlFormatException">A digit is missing or not hexadecimal.</exception>
    private int ReadHex(int count)
    {
        var value = 0;
        for (var i = 0; i < count; i++)
        {
            var b = Peek();
            var d = b switch
            {
                >= (byte)'0' and <= (byte)'9' => b - (byte)'0',
                >= (byte)'a' and <= (byte)'f' => b - (byte)'a' + 10,
                >= (byte)'A' and <= (byte)'F' => b - (byte)'A' + 10,
                _ => -1,
            };

            if (d < 0)
                throw Error(YamlResourceStrings.Format_Invalid_YamlInvalidEscape);

            value = (value << 4) | d;
            Advance();
        }

        return value;
    }

    /// <summary>
    /// Appends a Unicode code point to the byte buffer as UTF-8.
    /// </summary>
    /// <param name="buf">The output byte buffer.</param>
    /// <param name="codePoint">The code point to encode.</param>
    private static void AppendRune(List<byte> buf, int codePoint)
    {
        if (codePoint <= 0x7F)
        {
            buf.Add((byte)codePoint);
        }
        else if (codePoint <= 0x7FF)
        {
            buf.Add((byte)(0xC0 | (codePoint >> 6)));
            buf.Add((byte)(0x80 | (codePoint & 0x3F)));
        }
        else if (codePoint <= 0xFFFF)
        {
            buf.Add((byte)(0xE0 | (codePoint >> 12)));
            buf.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
            buf.Add((byte)(0x80 | (codePoint & 0x3F)));
        }
        else
        {
            buf.Add((byte)(0xF0 | (codePoint >> 18)));
            buf.Add((byte)(0x80 | ((codePoint >> 12) & 0x3F)));
            buf.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
            buf.Add((byte)(0x80 | (codePoint & 0x3F)));
        }
    }

    /// <summary>
    /// Emits the folded separator for accumulated line breaks: a space for one, newlines for more.
    /// </summary>
    /// <param name="buf">The output byte buffer.</param>
    /// <param name="pendingBreaks">The number of accumulated line breaks; reset to zero on return.</param>
    private static void FlushFold(List<byte> buf, ref int pendingBreaks)
    {
        if (pendingBreaks == 0)
            return;

        if (pendingBreaks == 1)
        {
            buf.Add((byte)' ');
        }
        else
        {
            for (var i = 0; i < pendingBreaks - 1; i++)
                buf.Add((byte)'\n');
        }

        pendingBreaks = 0;
    }

    /// <summary>
    /// Removes trailing inline space and tab bytes from the buffer before a folded line break.
    /// </summary>
    /// <param name="buf">The output byte buffer.</param>
    private static void TrimTrailingInlineSpace(List<byte> buf)
    {
        while (buf.Count > 0 && buf[^1] is (byte)' ' or (byte)'\t')
            buf.RemoveAt(buf.Count - 1);
    }

    /// <summary>
    /// Parses a literal (<c>|</c>) or folded (<c>&gt;</c>) block scalar, including the chomping and indentation
    /// indicators in the header, and applies the indentation and chomping rules to the collected lines.
    /// </summary>
    /// <param name="parentIndent">The indentation of the node that introduces the block scalar.</param>
    /// <param name="anchor">The anchor captured for the scalar, or <see langword="null" />.</param>
    /// <param name="tag">The tag captured for the scalar, or <see langword="null" />.</param>
    /// <returns>The row index of the scalar.</returns>
    private int ParseBlockScalar(int parentIndent, string? anchor, string? tag)
    {
        var offset = _pos;
        var folded = Peek() == (byte)'>';
        Advance();

        var explicitIndent = 0;
        var chomping = YamlBlockChomping.Clip;
        for (var i = 0; i < 2; i++)
        {
            var b = Peek();
            if (b is >= (byte)'1' and <= (byte)'9')
            {
                explicitIndent = b - (byte)'0';
                Advance();
            }
            else if (b == (byte)'+')
            {
                chomping = YamlBlockChomping.Keep;
                Advance();
            }
            else if (b == (byte)'-')
            {
                chomping = YamlBlockChomping.Strip;
                Advance();
            }
            else
            {
                break;
            }
        }

        SkipLineTrailing(); // consume the rest of the header line and its break

        var lines = new List<string>();
        var contentIndent = explicitIndent > 0 ? parentIndent + explicitIndent : -1;

        while (!AtEnd)
        {
            var lineStart = _pos;
            var p = _pos;
            while (p < _length && _source[p] == (byte)' ')
                p++;

            var blank = p >= _length || _source[p] is (byte)'\n' or (byte)'\r';
            if (!blank)
            {
                var col = p - lineStart;
                if (col == 0 && IsBoundaryAt(p))
                    break;

                if (contentIndent < 0)
                    contentIndent = col;

                if (col < contentIndent)
                    break;
            }

            var lineEnd = p;
            while (lineEnd < _length && _source[lineEnd] is not ((byte)'\n' or (byte)'\r'))
                lineEnd++;

            if (blank)
            {
                lines.Add(string.Empty);
            }
            else
            {
                var cstart = lineStart + contentIndent;
                lines.Add(Utf8(cstart, lineEnd - cstart));
            }

            _pos = lineEnd;
            if (!AtEnd)
                Advance();
        }

        var text = folded ? AssembleFolded(lines, chomping) : AssembleLiteral(lines, chomping);
        return NewString(text, folded ? YamlScalarStyle.Folded : YamlScalarStyle.Literal, offset, anchor, tag);
    }

    /// <summary>
    /// Determines whether a document boundary marker begins at the given source offset.
    /// </summary>
    /// <param name="p">The byte offset to test.</param>
    /// <returns><see langword="true" /> when <c>---</c> or <c>...</c> begins at the offset.</returns>
    private bool IsBoundaryAt(int p)
    {
        if (p + 3 > _length)
            return false;

        var b = _source[p];
        if (b is not ((byte)'-' or (byte)'.'))
            return false;

        if (_source[p + 1] != b || _source[p + 2] != b)
            return false;

        return p + 3 == _length || _source[p + 3] is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r';
    }

    /// <summary>
    /// Assembles the collected lines of a literal block scalar and applies the chomping indicator.
    /// </summary>
    /// <param name="lines">The content lines, with blank lines represented as empty strings.</param>
    /// <param name="chomping">The chomping mode from the header.</param>
    /// <returns>The assembled scalar text.</returns>
    private static string AssembleLiteral(List<string> lines, YamlBlockChomping chomping)
    {
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            sb.Append(line);
            sb.Append('\n');
        }

        return ApplyChomping(sb.ToString(), chomping);
    }

    /// <summary>
    /// Assembles the collected lines of a folded block scalar and applies the chomping indicator.
    /// </summary>
    /// <param name="lines">The content lines, with blank lines represented as empty strings.</param>
    /// <param name="chomping">The chomping mode from the header.</param>
    /// <returns>The assembled scalar text.</returns>
    private static string AssembleFolded(List<string> lines, YamlBlockChomping chomping)
    {
        var sb = new StringBuilder();
        var pending = 0;
        var any = false;
        var moreIndentedPrev = false;

        foreach (var line in lines)
        {
            if (line.Length == 0)
            {
                pending++;
                continue;
            }

            var moreIndented = line[0] is ' ' or '\t';
            if (any)
            {
                if (pending > 0)
                    sb.Append('\n', pending);
                else if (moreIndented || moreIndentedPrev)
                    sb.Append('\n');
                else
                    sb.Append(' ');
            }

            sb.Append(line);
            pending = 0;
            any = true;
            moreIndentedPrev = moreIndented;
        }

        // Re-attach trailing line breaks for chomping: one for the final content line plus any trailing blanks.
        var body = sb.ToString();
        var withTrailing = body + new string('\n', pending + (any ? 1 : 0));
        return ApplyChomping(withTrailing, chomping);
    }

    /// <summary>
    /// Applies the chomping indicator to a block scalar body that ends with its full trailing line breaks.
    /// </summary>
    /// <param name="text">The assembled text including all trailing line breaks.</param>
    /// <param name="chomping">The chomping mode.</param>
    /// <returns>The chomped text.</returns>
    private static string ApplyChomping(string text, YamlBlockChomping chomping)
    {
        switch (chomping)
        {
            case YamlBlockChomping.Strip:
                return text.TrimEnd('\n');

            case YamlBlockChomping.Keep:
                return text;

            default:
                var trimmed = text.TrimEnd('\n');
                return trimmed.Length == 0 ? string.Empty : trimmed + "\n";
        }
    }
}
