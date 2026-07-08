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
    /// <summary>Indicates whether the most recent plain-scalar line scan stopped at a trailing comment, ending the scalar.</summary>
    private bool _plainCommentTerminated;

    /// <summary>
    /// Parses a scalar node in block context, dispatching to quoted or plain parsing, and resolves the implicit type of
    /// single-line plain scalars.
    /// </summary>
    /// <param name="minIndent">The minimum continuation indentation for a multi-line plain scalar.</param>
    /// <returns>The row index of the scalar.</returns>
    private int ParseBlockScalarPlainOrQuoted(int minIndent)
    {
        int offset = _pos;
        byte c = Peek();

        // A plain scalar cannot begin with a flow-entry comma or a reserved indicator.
        if (c is (byte)',' or (byte)'%' or (byte)'@' or (byte)'`')
            throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

        if (c == (byte)'"')
        {
            int row = NewString(ReadDoubleQuoted(minIndent), YamlScalarStyle.DoubleQuoted, offset, null, null);
            SkipLineTrailing();
            return row;
        }

        if (c == (byte)'\'')
        {
            int row = NewString(ReadSingleQuoted(minIndent), YamlScalarStyle.SingleQuoted, offset, null, null);
            SkipLineTrailing();
            return row;
        }

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
        int firstStart = _pos;
        int firstEnd = ScanPlainLineEnd();

        bool folded = false;
        StringBuilder? sb = null;

        // A trailing comment terminates a plain scalar; it cannot continue onto following lines.
        while (!_plainCommentTerminated)
        {
            int savePos = _pos;
            int saveLine = _line;
            int saveLineStart = _lineStart;

            if (AtEnd)
                break;

            Advance(); // consume the line break ending the previous content line

            int blanks = 0;
            while (true)
            {
                int lineStart = _pos;
                int p = _pos;
                while (p < _length && _source[p] is (byte)' ' or (byte)'\t')
                    p++;

                bool blank = p >= _length || _source[p] is (byte)'\n' or (byte)'\r';
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

                int col = p - lineStart;
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

            int contStart = _pos;
            int contEnd = ScanPlainLineEnd();
            sb.Append(Utf8(contStart, contEnd - contStart));

            if (_plainCommentTerminated)
                break;
        }

    done:
        if (!folded)
        {
            ReadOnlySpan<byte> span = _source.AsSpan(firstStart, firstEnd - firstStart);
            YamlValueKind kind = YamlScalarResolver.Resolve(span, _version, out long bits);
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
        _plainCommentTerminated = false;
        int end = _pos;
        while (!IsBreakOrEnd(Peek()))
        {
            if (Peek() is (byte)' ' or (byte)'\t' && PeekAt(1) == (byte)'#')
            {
                _plainCommentTerminated = true;
                break;
            }

            // A ' : ' mapping value indicator cannot appear inside a plain scalar; a trailing ':' at end of line is
            // preserved as content.
            if (Peek() == (byte)':' && PeekAt(1) is (byte)' ' or (byte)'\t')
                throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

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
    /// <param name="minIndent">The minimum indentation required of continuation lines.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="YamlFormatException">The scalar is not terminated.</exception>
    private string ReadSingleQuoted(int minIndent)
    {
        Advance(); // opening quote
        var buf = new List<byte>();
        int pendingBreaks = 0;

        while (true)
        {
            if (AtEnd)
                throw Error(YamlResourceStrings.Format_Invalid_YamlUnterminatedScalar);

            byte b = Peek();
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

                FlushFold(buf, ref pendingBreaks);
                Advance();
                break;
            }

            if (b is (byte)'\n' or (byte)'\r')
            {
                TrimTrailingInlineSpace(buf, 0);
                Advance();
                if (IsBoundaryAt(_pos))
                    throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

                RequireQuotedContinuationIndent(minIndent);
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
    /// <param name="minIndent">The minimum indentation required of continuation lines.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="YamlFormatException">The scalar is not terminated or contains an invalid escape.</exception>
    private string ReadDoubleQuoted(int minIndent)
    {
        Advance(); // opening quote
        var buf = new List<byte>();
        int pendingBreaks = 0;

        // Bytes contributed by an escape are content and must survive trailing-whitespace folding; the protected length
        // marks how far into the buffer such bytes reach.
        int protectedLength = 0;

        while (true)
        {
            if (AtEnd)
                throw Error(YamlResourceStrings.Format_Invalid_YamlUnterminatedScalar);

            byte b = Peek();
            if (b == (byte)'"')
            {
                FlushFold(buf, ref pendingBreaks);
                Advance();
                break;
            }

            if (b == (byte)'\\')
            {
                byte next = PeekAt(1);
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
                protectedLength = buf.Count;
                continue;
            }

            if (b is (byte)'\n' or (byte)'\r')
            {
                TrimTrailingInlineSpace(buf, protectedLength);
                Advance();
                if (IsBoundaryAt(_pos))
                    throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlUnexpectedContent);

                RequireQuotedContinuationIndent(minIndent);
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
        byte e = Peek();
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
            case (byte)'x': AppendRune(buf, ValidateEscapeCodePoint(ReadHex(2))); break;
            case (byte)'u': AppendRune(buf, ValidateEscapeCodePoint(ReadHex(4))); break;
            case (byte)'U': AppendRune(buf, ValidateEscapeCodePoint(ReadHex(8))); break;
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
        int value = 0;
        for (int i = 0; i < count; i++)
        {
            byte b = Peek();
            int d = b switch
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
    /// Validates that a Unicode escape resolves to a legal scalar value, rejecting surrogates and out-of-range code
    /// points.
    /// </summary>
    /// <param name="codePoint">The decoded escape code point.</param>
    /// <returns>The validated code point.</returns>
    /// <exception cref="YamlFormatException">
    /// The code point is a surrogate (<c>U+D800</c>–<c>U+DFFF</c>) or exceeds <c>U+10FFFF</c>.
    /// </exception>
    private int ValidateEscapeCodePoint(int codePoint)
    {
        if (codePoint > 0x10FFFF || codePoint is >= 0xD800 and <= 0xDFFF)
            throw Error(YamlResourceStrings.Format_Invalid_YamlInvalidEscape);

        return codePoint;
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
            for (int i = 0; i < pendingBreaks - 1; i++)
                buf.Add((byte)'\n');
        }

        pendingBreaks = 0;
    }


    /// <summary>
    /// Validates the indentation of a quoted-scalar continuation line: it must use spaces only and be indented more
    /// than the scalar's parent node.
    /// </summary>
    /// <param name="minIndent">
    /// The minimum content column for a continuation line, or a negative value to skip the check.
    /// </param>
    /// <exception cref="YamlFormatException">
    /// A continuation line uses a tab for indentation or is under-indented.
    /// </exception>
    private void RequireQuotedContinuationIndent(int minIndent)
    {
        if (minIndent <= 0)
            return;

        int p = _pos;
        int spaces = 0;
        while (p < _length && spaces < minIndent && _source[p] == (byte)' ')
        {
            p++;
            spaces++;
        }

        // A blank line imposes no indentation requirement.
        if (p >= _length || _source[p] is (byte)'\n' or (byte)'\r')
            return;

        // A tab within the required indentation, or too few indentation spaces, under-indents the continuation.
        if (_source[p] == (byte)'\t' && spaces < minIndent)
            throw ErrorAt(p, YamlResourceStrings.Format_Invalid_YamlTabIndentation);

        if (spaces < minIndent)
            throw ErrorAt(_pos, YamlResourceStrings.Format_Invalid_YamlInvalidIndentation);
    }

    /// <summary>
    /// Removes trailing literal space and tab bytes from the buffer before a folded line break, without trimming below
    /// the protected length (which marks bytes contributed by an escape and therefore part of the content).
    /// </summary>
    /// <param name="buf">The output byte buffer.</param>
    /// <param name="protectedLength">The buffer length below which trimming must not reach.</param>
    private static void TrimTrailingInlineSpace(List<byte> buf, int protectedLength)
    {
        while (buf.Count > protectedLength && buf[^1] is (byte)' ' or (byte)'\t')
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
        int offset = _pos;
        bool folded = Peek() == (byte)'>';
        Advance();

        int explicitIndent = 0;
        YamlBlockChomping chomping = YamlBlockChomping.Clip;
        for (int i = 0; i < 2; i++)
        {
            byte b = Peek();
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

        // Each raw line records whether it was blank and its leading-space count, so a blank line's content beyond the
        // (possibly later-detected) content indentation can be preserved once that indentation is known.
        var raw = new List<(bool Blank, int Start, int End, int Spaces)>();
        int contentIndent = explicitIndent > 0 ? parentIndent + explicitIndent : -1;

        while (!AtEnd)
        {
            int lineStart = _pos;
            int p = _pos;
            while (p < _length && _source[p] == (byte)' ')
                p++;

            int spaces = p - lineStart;
            bool blank = p >= _length || _source[p] is (byte)'\n' or (byte)'\r';

            // A tab at or below the parent indentation cannot serve as block-scalar indentation.
            if (!blank && _source[p] == (byte)'\t' && spaces <= parentIndent)
                throw ErrorAt(p, YamlResourceStrings.Format_Invalid_YamlTabIndentation);

            if (!blank)
            {
                // A line no more indented than the parent (including a document marker) ends the block scalar.
                if (spaces <= parentIndent || (spaces == 0 && IsBoundaryAt(p)))
                {
                    _pos = lineStart;
                    break;
                }

                if (contentIndent < 0)
                    contentIndent = spaces;

                if (spaces < contentIndent)
                {
                    _pos = lineStart;
                    break;
                }
            }

            int lineEnd = p;
            while (lineEnd < _length && _source[lineEnd] is not ((byte)'\n' or (byte)'\r'))
                lineEnd++;

            raw.Add((blank, lineStart, lineEnd, spaces));

            _pos = lineEnd;
            if (!AtEnd)
                Advance();
        }

        // A leading empty line must not be more indented than the detected content indentation.
        if (contentIndent >= 0)
        {
            foreach ((bool blank, int start, int _, int spaces) in raw)
            {
                if (!blank)
                    break;

                if (spaces > contentIndent)
                    throw ErrorAt(start, YamlResourceStrings.Format_Invalid_YamlInvalidBlockScalar);
            }
        }

        var lines = new List<(bool Blank, string Text)>(raw.Count);
        foreach ((bool blank, int start, int end, int spaces) in raw)
        {
            if (blank)
            {
                string extra = contentIndent >= 0 && spaces > contentIndent ? new string(' ', spaces - contentIndent) : string.Empty;
                lines.Add((true, extra));
            }
            else
            {
                int cstart = start + contentIndent;
                lines.Add((false, Utf8(cstart, end - cstart)));
            }
        }

        string text = folded ? AssembleFolded(lines, chomping) : AssembleLiteral(lines, chomping);
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

        byte b = _source[p];
        if (b is not ((byte)'-' or (byte)'.'))
            return false;

        if (_source[p + 1] != b || _source[p + 2] != b)
            return false;

        return p + 3 == _length || _source[p + 3] is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r';
    }

    /// <summary>
    /// Assembles the collected lines of a literal block scalar and applies the chomping indicator.
    /// </summary>
    /// <param name="lines">The content lines, each flagged as blank and carrying its indentation-stripped text.</param>
    /// <param name="chomping">The chomping mode from the header.</param>
    /// <returns>The assembled scalar text.</returns>
    private static string AssembleLiteral(List<(bool Blank, string Text)> lines, YamlBlockChomping chomping)
    {
        var sb = new StringBuilder();
        foreach ((bool _, string? text) in lines)
        {
            sb.Append(text);
            sb.Append('\n');
        }

        return ApplyChomping(sb.ToString(), chomping);
    }

    /// <summary>
    /// Assembles the collected lines of a folded block scalar and applies the chomping indicator.
    /// </summary>
    /// <param name="lines">The content lines, each flagged as blank and carrying its indentation-stripped text.</param>
    /// <param name="chomping">The chomping mode from the header.</param>
    /// <returns>The assembled scalar text.</returns>
    private static string AssembleFolded(List<(bool Blank, string Text)> lines, YamlBlockChomping chomping)
    {
        var sb = new StringBuilder();
        int pending = 0;
        bool any = false;
        bool prevMoreIndented = false;

        foreach ((bool blank, string? text) in lines)
        {
            if (blank)
            {
                pending++;
                continue;
            }

            bool moreIndented = text.Length > 0 && text[0] is ' ' or '\t';
            if (any)
            {
                if (moreIndented || prevMoreIndented)
                    sb.Append('\n', pending + 1); // breaks adjacent to a more-indented line are all preserved
                else if (pending > 0)
                    sb.Append('\n', pending); // a run of blank lines preserves that many breaks, folding the join
                else
                    sb.Append(' '); // an ordinary single line break folds to a space
            }
            else if (pending > 0)
            {
                sb.Append('\n', pending); // leading blank lines before the first content line
            }

            sb.Append(text);
            pending = 0;
            any = true;
            prevMoreIndented = moreIndented;
        }

        // Re-attach trailing line breaks for chomping: one for the final content line plus any trailing blanks.
        string withTrailing = sb.ToString() + new string('\n', pending + (any ? 1 : 0));
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
                string trimmed = text.TrimEnd('\n');
                return trimmed.Length == 0 ? string.Empty : trimmed + "\n";
        }
    }
}
