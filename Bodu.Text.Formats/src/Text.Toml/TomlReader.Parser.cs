// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReader.Parser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Text.Toml;

public partial class TomlReader
{
    /// <summary>
    /// A single-pass recursive-descent parser that materializes TOML source text into a <see cref="TomlTable" />
    /// document, enforcing the specification's key, value, table, and array-of-tables rules for the configured
    /// <see cref="TomlSpecVersion" />.
    /// </summary>
    private sealed class Parser
    {
        /// <summary>
        /// The number of 100-nanosecond ticks in one second.
        /// </summary>
        private const long TicksPerSecond = 10_000_000L;

        /// <summary>
        /// The source text being parsed.
        /// </summary>
        private readonly string _src;

        /// <summary>
        /// The root table of the document.
        /// </summary>
        private readonly TomlTable _root = new();

        /// <summary>
        /// Tables explicitly defined by a <c>[table]</c> header.
        /// </summary>
        private readonly HashSet<TomlTable> _headerDefined = new();

        /// <summary>
        /// Tables created implicitly as intermediates of a dotted key.
        /// </summary>
        private readonly HashSet<TomlTable> _dotted = new();

        /// <summary>
        /// Inline tables, which are closed to further extension once defined.
        /// </summary>
        private readonly HashSet<TomlTable> _inline = new();

        /// <summary>
        /// Super-tables created implicitly by a table-header path, which a later header may promote.
        /// </summary>
        private readonly HashSet<TomlTable> _implicitSuper = new();

        /// <summary>
        /// Arrays created by an array-of-tables header, to which further elements may be appended.
        /// </summary>
        private readonly HashSet<TomlArray> _tableArrays = new();

        /// <summary>
        /// The TOML specification version whose grammar features the parser enforces.
        /// </summary>
        private readonly TomlSpecVersion _specVersion;

        /// <summary>
        /// The cursor position within <see cref="_src" />.
        /// </summary>
        private int _pos;

        /// <summary>
        /// The current 1-based source line.
        /// </summary>
        private int _line = 1;

        /// <summary>
        /// The table that bare key/value pairs are currently assigned to.
        /// </summary>
        private TomlTable _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser" /> class over the supplied source.
        /// </summary>
        /// <param name="source">The TOML source text.</param>
        /// <param name="specVersion">The specification version whose grammar to enforce.</param>
        public Parser(ReadOnlySpan<char> source, TomlSpecVersion specVersion)
        {
            _src = source.ToString();
            _current = _root;
            _specVersion = specVersion;
        }

        /// <summary>
        /// Gets a value indicating whether the cursor is at the end of the source.
        /// </summary>
        /// <returns><see langword="true" /> when no characters remain.</returns>
        private bool Eof => _pos >= _src.Length;

        /// <summary>
        /// Gets the character at the cursor.
        /// </summary>
        /// <returns>The current character.</returns>
        private char Current => _src[_pos];

        /// <summary>
        /// Parses the document and returns its root table.
        /// </summary>
        /// <returns>The root <see cref="TomlTable" />.</returns>
        /// <exception cref="TomlFormatException">Thrown when the source is not valid TOML.</exception>
        public TomlTable Parse()
        {
            // A leading UTF-8 byte-order mark, when decoded, appears as U+FEFF; skip it.
            if (!Eof && Current == '\uFEFF')
                Advance();

            SkipToNextExpression();
            while (!Eof)
            {
                if (Current == '[')
                    ParseTableHeader();
                else
                    ParseKeyValue(_current);

                SkipInlineWhitespace();
                if (!Eof && Current == '#')
                    SkipComment();

                if (!Eof)
                {
                    if (!AtNewline())
                        throw Error(FormatsResourceStrings.Format_Invalid_TomlExpectedNewline);
                    ConsumeNewline();
                }

                SkipToNextExpression();
            }

            return _root;
        }

        /// <summary>
        /// Returns the character at <paramref name="offset" /> from the cursor, or <c>'\0'</c> past the end.
        /// </summary>
        /// <param name="offset">The look-ahead offset.</param>
        /// <returns>The character, or <c>'\0'</c>.</returns>
        private char Peek(int offset = 0) =>
            _pos + offset < _src.Length ? _src[_pos + offset] : '\0';

        /// <summary>
        /// Advances the cursor by one character.
        /// </summary>
        private void Advance() => _pos++;

        /// <summary>
        /// Indicates whether the cursor is positioned at a newline (LF or CRLF).
        /// </summary>
        /// <returns><see langword="true" /> when a newline begins at the cursor.</returns>
        private bool AtNewline() =>
            !Eof && (Current == '\n' || (Current == '\r' && Peek(1) == '\n'));

        /// <summary>
        /// Consumes a newline (LF or CRLF) and increments the line counter.
        /// </summary>
        private void ConsumeNewline()
        {
            if (Current == '\r')
                Advance();
            Advance();
            _line++;
        }

        /// <summary>
        /// Skips spaces and tabs at the cursor.
        /// </summary>
        private void SkipInlineWhitespace()
        {
            while (!Eof && (Current == ' ' || Current == '\t'))
                Advance();
        }

        /// <summary>
        /// Skips a comment from the <c>#</c> to the end of the line, rejecting disallowed control characters.
        /// </summary>
        private void SkipComment()
        {
            Advance();
            while (!Eof && !AtNewline())
            {
                var c = Current;
                if (c == '\r')
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlControlCharacter);
                if (c == 0x7F || (c < 0x20 && c != 0x09))
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlControlCharacter);
                Advance();
            }
        }

        /// <summary>
        /// Skips blank lines, comment lines, and leading whitespace until the next real expression or the end of input.
        /// </summary>
        private void SkipToNextExpression()
        {
            while (!Eof)
            {
                SkipInlineWhitespace();
                if (Eof)
                    return;
                if (Current == '#')
                {
                    SkipComment();
                    if (!Eof)
                        ConsumeNewline();
                    continue;
                }

                if (AtNewline())
                {
                    ConsumeNewline();
                    continue;
                }

                return;
            }
        }

        /// <summary>
        /// Skips whitespace, comments, and newlines — the <c>ws-comment-newline</c> production used inside arrays (and,
        /// in TOML v1.1.0, inside inline tables).
        /// </summary>
        private void SkipValueSeparators()
        {
            while (!Eof)
            {
                if (Current == ' ' || Current == '\t')
                {
                    Advance();
                }
                else if (Current == '#')
                {
                    SkipComment();
                }
                else if (AtNewline())
                {
                    ConsumeNewline();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Skips the separators permitted between inline-table tokens. TOML v1.1.0 permits the full
        /// whitespace/comment/newline production; strict TOML v1.0.0 permits only spaces and tabs.
        /// </summary>
        private void SkipInlineTableSeparators()
        {
            if (_specVersion == TomlSpecVersion.V1_1)
                SkipValueSeparators();
            else
                SkipInlineWhitespace();
        }

        /// <summary>
        /// Parses a standard table header or an array-of-tables header.
        /// </summary>
        private void ParseTableHeader()
        {
            Advance();
            var arrayTable = false;
            if (!Eof && Current == '[')
            {
                arrayTable = true;
                Advance();
            }

            SkipInlineWhitespace();
            var path = ParseKeyPath();
            SkipInlineWhitespace();

            if (Eof || Current != ']')
                throw Error(FormatsResourceStrings.Format_Invalid_TomlUnterminatedTable);
            Advance();

            if (arrayTable)
            {
                if (Eof || Current != ']')
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlUnterminatedTable);
                Advance();
                DefineArrayTable(path);
            }
            else
            {
                DefineStandardTable(path);
            }
        }

        /// <summary>
        /// Parses a key/value pair and assigns it under <paramref name="target" />.
        /// </summary>
        /// <param name="target">The table receiving the assignment.</param>
        private void ParseKeyValue(TomlTable target)
        {
            var path = ParseKeyPath();
            SkipInlineWhitespace();
            if (Eof || Current != '=')
                throw Error(FormatsResourceStrings.Format_Invalid_TomlExpectedEquals);
            Advance();
            SkipInlineWhitespace();

            var value = ParseValue();
            AssignKeyValue(target, path, value);
        }

        /// <summary>
        /// Parses a dotted key path (one or more simple keys joined by dots).
        /// </summary>
        /// <returns>The key segments in order.</returns>
        private List<string> ParseKeyPath()
        {
            var keys = new List<string> { ParseSimpleKey() };
            while (true)
            {
                SkipInlineWhitespace();
                if (!Eof && Current == '.')
                {
                    Advance();
                    SkipInlineWhitespace();
                    keys.Add(ParseSimpleKey());
                }
                else
                {
                    break;
                }
            }

            return keys;
        }

        /// <summary>
        /// Parses a single bare, basic-quoted, or literal-quoted key.
        /// </summary>
        /// <returns>The key text.</returns>
        private string ParseSimpleKey()
        {
            if (Eof)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlExpectedKey);

            var c = Current;
            if (c == '"')
            {
                if (Peek(1) == '"' && Peek(2) == '"')
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlMultilineKey);
                return ParseBasicString();
            }

            if (c == '\'')
            {
                if (Peek(1) == '\'' && Peek(2) == '\'')
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlMultilineKey);
                return ParseLiteralString();
            }

            var start = _pos;
            while (!Eof && IsBareKeyChar(Current))
                Advance();

            if (_pos == start)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlExpectedKey);

            return _src.Substring(start, _pos - start);
        }

        /// <summary>
        /// Indicates whether <paramref name="c" /> is a legal bare-key character.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns><see langword="true" /> when the character is in <c>A-Za-z0-9_-</c>.</returns>
        private static bool IsBareKeyChar(char c) =>
            (c is >= 'A' and <= 'Z') || (c is >= 'a' and <= 'z') || (c is >= '0' and <= '9') || c == '_' || c == '-';

        /// <summary>
        /// Parses a value of any TOML type at the cursor.
        /// </summary>
        /// <returns>The parsed value.</returns>
        private TomlValue ParseValue()
        {
            if (Eof)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlExpectedValue);

            switch (Current)
            {
                case '"':
                    return new TomlString(Peek(1) == '"' && Peek(2) == '"' ? ParseMultilineBasicString() : ParseBasicString());
                case '\'':
                    return new TomlString(Peek(1) == '\'' && Peek(2) == '\'' ? ParseMultilineLiteralString() : ParseLiteralString());
                case '[':
                    return ParseArray();
                case '{':
                    return ParseInlineTable();
                case 't':
                case 'f':
                    return ParseBoolean();
                default:
                    return ParseNumberOrDateTime();
            }
        }

        /// <summary>
        /// Parses a boolean literal.
        /// </summary>
        /// <returns>The boolean value.</returns>
        private TomlValue ParseBoolean()
        {
            if (TryConsumeKeyword("true"))
                return new TomlBoolean(true);
            if (TryConsumeKeyword("false"))
                return new TomlBoolean(false);
            throw Error(FormatsResourceStrings.Format_Invalid_TomlExpectedValue);
        }

        /// <summary>
        /// Consumes <paramref name="keyword" /> when it appears at the cursor followed by a value terminator.
        /// </summary>
        /// <param name="keyword">The keyword to match.</param>
        /// <returns><see langword="true" /> when the keyword was consumed.</returns>
        private bool TryConsumeKeyword(string keyword)
        {
            if (_pos + keyword.Length > _src.Length)
                return false;
            if (string.CompareOrdinal(_src, _pos, keyword, 0, keyword.Length) != 0)
                return false;
            var after = _pos + keyword.Length;
            if (after < _src.Length && !IsValueTerminator(_src[after]))
                return false;

            _pos = after;
            return true;
        }

        /// <summary>
        /// Indicates whether <paramref name="c" /> ends a bare value token.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns><see langword="true" /> when the character terminates a value.</returns>
        private static bool IsValueTerminator(char c) =>
            c is ' ' or '\t' or '\r' or '\n' or ',' or ']' or '}' or '#';

        /// <summary>
        /// Parses a single-line basic string (its opening quote at the cursor).
        /// </summary>
        /// <returns>The decoded string.</returns>
        private string ParseBasicString()
        {
            Advance();
            var sb = new StringBuilder();
            while (true)
            {
                if (Eof || AtNewline())
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlUnterminatedString);

                var c = Current;
                if (c == '"')
                {
                    Advance();
                    return sb.ToString();
                }

                if (c == '\\')
                {
                    Advance();
                    AppendEscape(sb);
                    continue;
                }

                if (c == 0x7F || (c < 0x20 && c != 0x09))
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlControlCharacter);

                sb.Append(c);
                Advance();
            }
        }

        /// <summary>
        /// Parses a single-line literal string (its opening apostrophe at the cursor).
        /// </summary>
        /// <returns>The literal string.</returns>
        private string ParseLiteralString()
        {
            Advance();
            var start = _pos;
            while (true)
            {
                if (Eof || AtNewline())
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlUnterminatedString);

                var c = Current;
                if (c == '\'')
                {
                    var text = _src.Substring(start, _pos - start);
                    Advance();
                    return text;
                }

                if (c == 0x7F || (c < 0x20 && c != 0x09))
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlControlCharacter);

                Advance();
            }
        }

        /// <summary>
        /// Parses a multi-line basic string (its opening triple quote at the cursor).
        /// </summary>
        /// <returns>The decoded string.</returns>
        private string ParseMultilineBasicString()
        {
            _pos += 3;
            if (AtNewline())
                ConsumeNewline();

            var sb = new StringBuilder();
            while (true)
            {
                if (Eof)
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlUnterminatedString);

                var c = Current;
                if (c == '"')
                {
                    var run = CountRun('"');
                    if (run >= 3)
                    {
                        var content = run - 3;
                        if (content > 2)
                            throw Error(FormatsResourceStrings.Format_Invalid_TomlUnterminatedString);
                        sb.Append('"', content);
                        _pos += run;
                        return sb.ToString();
                    }

                    sb.Append('"', run);
                    _pos += run;
                    continue;
                }

                if (c == '\\')
                {
                    if (IsLineEndingBackslash())
                    {
                        ConsumeLineEndingBackslash();
                        continue;
                    }

                    Advance();
                    AppendEscape(sb);
                    continue;
                }

                if (AtNewline())
                {
                    sb.Append('\n');
                    ConsumeNewline();
                    continue;
                }

                if (c == 0x7F || (c < 0x20 && c != 0x09))
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlControlCharacter);

                sb.Append(c);
                Advance();
            }
        }

        /// <summary>
        /// Parses a multi-line literal string (its opening triple apostrophe at the cursor).
        /// </summary>
        /// <returns>The literal string.</returns>
        private string ParseMultilineLiteralString()
        {
            _pos += 3;
            if (AtNewline())
                ConsumeNewline();

            var sb = new StringBuilder();
            while (true)
            {
                if (Eof)
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlUnterminatedString);

                var c = Current;
                if (c == '\'')
                {
                    var run = CountRun('\'');
                    if (run >= 3)
                    {
                        var content = run - 3;
                        if (content > 2)
                            throw Error(FormatsResourceStrings.Format_Invalid_TomlUnterminatedString);
                        sb.Append('\'', content);
                        _pos += run;
                        return sb.ToString();
                    }

                    sb.Append('\'', run);
                    _pos += run;
                    continue;
                }

                if (AtNewline())
                {
                    sb.Append('\n');
                    ConsumeNewline();
                    continue;
                }

                if (c == 0x7F || (c < 0x20 && c != 0x09))
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlControlCharacter);

                sb.Append(c);
                Advance();
            }
        }

        /// <summary>
        /// Counts the run of identical characters <paramref name="c" /> starting at the cursor.
        /// </summary>
        /// <param name="c">The character to count.</param>
        /// <returns>The run length.</returns>
        private int CountRun(char c)
        {
            var n = 0;
            while (_pos + n < _src.Length && _src[_pos + n] == c)
                n++;
            return n;
        }

        /// <summary>
        /// Indicates whether the backslash at the cursor is a line-ending backslash (escape, optional whitespace, then
        /// a newline).
        /// </summary>
        /// <returns><see langword="true" /> when a line-ending backslash begins at the cursor.</returns>
        private bool IsLineEndingBackslash()
        {
            var j = _pos + 1;
            while (j < _src.Length && (_src[j] == ' ' || _src[j] == '\t'))
                j++;
            return j < _src.Length && (_src[j] == '\n' || (_src[j] == '\r' && j + 1 < _src.Length && _src[j + 1] == '\n'));
        }

        /// <summary>
        /// Consumes a line-ending backslash and all following whitespace and newlines.
        /// </summary>
        private void ConsumeLineEndingBackslash()
        {
            Advance();
            while (!Eof && (Current == ' ' || Current == '\t'))
                Advance();
            ConsumeNewline();
            while (!Eof && (Current == ' ' || Current == '\t' || AtNewline()))
            {
                if (AtNewline())
                    ConsumeNewline();
                else
                    Advance();
            }
        }

        /// <summary>
        /// Appends the escape sequence beginning at the cursor (which is positioned just after the backslash).
        /// </summary>
        /// <param name="sb">The destination builder.</param>
        private void AppendEscape(StringBuilder sb)
        {
            if (Eof)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidEscape);

            var e = Current;

            // The \e (escape) and \xHH (hex byte) escapes were introduced in TOML v1.1.0; reject them under v1.0.
            if (_specVersion != TomlSpecVersion.V1_1 && (e == 'e' || e == 'x'))
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidEscape);

            switch (e)
            {
                case '"': sb.Append('"'); Advance(); break;
                case '\\': sb.Append('\\'); Advance(); break;
                case 'b': sb.Append('\b'); Advance(); break;
                case 't': sb.Append('\t'); Advance(); break;
                case 'n': sb.Append('\n'); Advance(); break;
                case 'f': sb.Append('\f'); Advance(); break;
                case 'r': sb.Append('\r'); Advance(); break;
                case 'e': sb.Append('\u001b'); Advance(); break;
                case 'x': Advance(); AppendUnicodeEscape(sb, 2); break;
                case 'u': Advance(); AppendUnicodeEscape(sb, 4); break;
                case 'U': Advance(); AppendUnicodeEscape(sb, 8); break;
                default: throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidEscape);
            }
        }

        /// <summary>
        /// Reads <paramref name="digits" /> hexadecimal digits and appends the corresponding Unicode scalar value.
        /// </summary>
        /// <param name="sb">The destination builder.</param>
        /// <param name="digits">The number of hexadecimal digits to read.</param>
        private void AppendUnicodeEscape(StringBuilder sb, int digits)
        {
            if (_pos + digits > _src.Length)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidEscape);

            long value = 0;
            for (var i = 0; i < digits; i++)
            {
                var d = HexValue(_src[_pos + i]);
                if (d < 0)
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidEscape);
                value = (value << 4) | (uint)d;
            }

            _pos += digits;

            if (value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF))
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidEscape);

            sb.Append(char.ConvertFromUtf32((int)value));
        }

        /// <summary>
        /// Returns the numeric value of a hexadecimal digit, or <c>-1</c> when the character is not a hex digit.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>The hex value, or <c>-1</c>.</returns>
        private static int HexValue(char c) =>
            c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => -1,
            };

        /// <summary>
        /// Parses an array value (its opening bracket at the cursor).
        /// </summary>
        /// <returns>The array.</returns>
        private TomlArray ParseArray()
        {
            Advance();
            var array = new TomlArray();
            while (true)
            {
                SkipValueSeparators();
                if (Eof)
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidArray);
                if (Current == ']')
                {
                    Advance();
                    return array;
                }

                array.Add(ParseValue());
                SkipValueSeparators();

                if (!Eof && Current == ',')
                {
                    Advance();
                    continue;
                }

                if (!Eof && Current == ']')
                {
                    Advance();
                    return array;
                }

                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidArray);
            }
        }

        /// <summary>
        /// Parses an inline table (its opening brace at the cursor). TOML v1.1.0 permits the multi-line and
        /// trailing-comma forms; strict TOML v1.0.0 requires a single-line table with no trailing comma.
        /// </summary>
        /// <returns>The inline table.</returns>
        private TomlTable ParseInlineTable()
        {
            Advance();
            var table = new TomlTable();
            SkipInlineTableSeparators();
            if (!Eof && Current == '}')
            {
                Advance();
                _inline.Add(table);
                return table;
            }

            while (true)
            {
                ParseKeyValue(table);
                SkipInlineTableSeparators();

                if (Eof)
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidInlineTable);

                if (Current == ',')
                {
                    Advance();
                    SkipInlineTableSeparators();
                    if (!Eof && Current == '}')
                    {
                        if (_specVersion != TomlSpecVersion.V1_1)
                            throw Error(FormatsResourceStrings.Format_Invalid_TomlInlineTableTrailingComma);

                        Advance();
                        break;
                    }

                    continue;
                }

                if (Current == '}')
                {
                    Advance();
                    break;
                }

                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidInlineTable);
            }

            _inline.Add(table);
            return table;
        }

        /// <summary>
        /// Parses a numeric value, or a date-time value when the cursor matches an RFC 3339 prefix.
        /// </summary>
        /// <returns>The parsed value.</returns>
        private TomlValue ParseNumberOrDateTime()
        {
            if (IsDigit(Peek(0)) && IsDigit(Peek(1)) && IsDigit(Peek(2)) && IsDigit(Peek(3)) && Peek(4) == '-')
                return ParseDateTime();
            if (IsDigit(Peek(0)) && IsDigit(Peek(1)) && Peek(2) == ':')
                return ParseTimeValue();

            return ParseNumber();
        }

        /// <summary>
        /// Reads a bare value token (digits, signs, and number punctuation) up to the next value terminator.
        /// </summary>
        /// <returns>The token text.</returns>
        private string ReadBareToken()
        {
            var start = _pos;
            while (!Eof && !IsValueTerminator(Current))
                Advance();
            return _src.Substring(start, _pos - start);
        }

        /// <summary>
        /// Parses an integer or float token.
        /// </summary>
        /// <returns>The numeric value.</returns>
        private TomlValue ParseNumber()
        {
            var token = ReadBareToken();
            if (token.Length == 0)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlExpectedValue);

            switch (token)
            {
                case "inf":
                case "+inf":
                    return new TomlFloat(double.PositiveInfinity);
                case "-inf":
                    return new TomlFloat(double.NegativeInfinity);
                case "nan":
                case "+nan":
                case "-nan":
                    return new TomlFloat(double.NaN);
            }

            if (token.Length >= 2 && token[0] == '0')
            {
                switch (token[1])
                {
                    case 'x': return ParseRadixInteger(token[2..], 16);
                    case 'o': return ParseRadixInteger(token[2..], 8);
                    case 'b': return ParseRadixInteger(token[2..], 2);
                }
            }

            var hasDot = token.IndexOf('.') >= 0;
            var hasExp = token.IndexOf('e') >= 0 || token.IndexOf('E') >= 0;
            return hasDot || hasExp ? ParseFloat(token) : ParseDecimalInteger(token);
        }

        /// <summary>
        /// Parses a decimal integer token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>The integer value.</returns>
        private TomlValue ParseDecimalInteger(string token)
        {
            var i = 0;
            var negative = false;
            if (token[0] == '+' || token[0] == '-')
            {
                negative = token[0] == '-';
                i = 1;
            }

            var body = token[i..];
            if (!TryStripUnderscores(body, allowHexLetters: false, out var clean) || clean.Length == 0 || !AllAsciiDigits(clean))
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidNumber);
            if (clean.Length > 1 && clean[0] == '0')
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidNumber);

            if (!ulong.TryParse(clean, NumberStyles.None, CultureInfo.InvariantCulture, out var magnitude))
                throw Error(FormatsResourceStrings.Format_Invalid_TomlIntegerOutOfRange);

            if (negative)
            {
                if (magnitude > (ulong)long.MaxValue + 1)
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlIntegerOutOfRange);
                return new TomlInteger(magnitude == (ulong)long.MaxValue + 1 ? long.MinValue : -(long)magnitude);
            }

            if (magnitude > long.MaxValue)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlIntegerOutOfRange);
            return new TomlInteger((long)magnitude);
        }

        /// <summary>
        /// Parses a hexadecimal, octal, or binary integer body.
        /// </summary>
        /// <param name="body">The token body after the radix prefix.</param>
        /// <param name="radix">The numeric base (16, 8, or 2).</param>
        /// <returns>The integer value.</returns>
        private TomlValue ParseRadixInteger(string body, int radix)
        {
            if (!TryStripUnderscores(body, allowHexLetters: radix == 16, out var clean) || clean.Length == 0)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidNumber);

            ulong value = 0;
            foreach (var c in clean)
            {
                var d = HexValue(c);
                if (d < 0 || d >= radix)
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidNumber);
                if (value > (ulong.MaxValue - (ulong)d) / (ulong)radix)
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlIntegerOutOfRange);
                value = (value * (ulong)radix) + (ulong)d;
            }

            if (value > long.MaxValue)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlIntegerOutOfRange);
            return new TomlInteger((long)value);
        }

        /// <summary>
        /// Parses a floating-point token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>The float value.</returns>
        private TomlValue ParseFloat(string token)
        {
            if (!TryStripUnderscores(token, allowHexLetters: false, out var clean))
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidNumber);
            if (!IsValidFloatLiteral(clean))
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidNumber);

            var value = double.Parse(clean, NumberStyles.Float, CultureInfo.InvariantCulture);
            return new TomlFloat(value);
        }

        /// <summary>
        /// Validates that <paramref name="s" /> is a TOML float literal (underscores already removed).
        /// </summary>
        /// <param name="s">The candidate literal.</param>
        /// <returns><see langword="true" /> when the literal is a valid TOML float.</returns>
        private static bool IsValidFloatLiteral(string s)
        {
            var i = 0;
            if (i < s.Length && (s[i] == '+' || s[i] == '-'))
                i++;

            // Integer part: 0 or [1-9][0-9]*.
            var intStart = i;
            while (i < s.Length && IsDigit(s[i]))
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
                while (i < s.Length && IsDigit(s[i]))
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
                while (i < s.Length && IsDigit(s[i]))
                    i++;
                if (i == expStart)
                    return false;
            }

            return i == s.Length && (hasFraction || hasExponent);
        }

        /// <summary>
        /// Parses a date or date-time value beginning with a four-digit year.
        /// </summary>
        /// <returns>A local date, local date-time, or offset date-time value.</returns>
        private TomlValue ParseDateTime()
        {
            var year = ReadDateDigits(4);
            ExpectDateChar('-');
            var month = ReadDateDigits(2);
            ExpectDateChar('-');
            var day = ReadDateDigits(2);

            var hasTime = false;
            if (!Eof && (Current == 'T' || Current == 't'))
            {
                hasTime = true;
                Advance();
            }
            else if (!Eof && Current == ' ' && IsDigit(Peek(1)) && IsDigit(Peek(2)) && Peek(3) == ':')
            {
                hasTime = true;
                Advance();
            }

            if (!hasTime)
                return new TomlLocalDate(MakeDate(year, month, day));

            var (hour, minute, second, fractionTicks) = ReadPartialTime();

            if (!Eof && (Current == 'Z' || Current == 'z' || Current == '+' || Current == '-'))
            {
                var offset = ReadTimeOffset();
                var local = MakeDateTime(year, month, day, hour, minute, second, fractionTicks);
                try
                {
                    return new TomlOffsetDateTime(new DateTimeOffset(local, offset));
                }
                catch (ArgumentException)
                {
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidDateTime);
                }
            }

            return new TomlLocalDateTime(MakeDateTime(year, month, day, hour, minute, second, fractionTicks));
        }

        /// <summary>
        /// Parses a bare local-time value beginning with a two-digit hour.
        /// </summary>
        /// <returns>The local time value.</returns>
        private TomlValue ParseTimeValue()
        {
            var (hour, minute, second, fractionTicks) = ReadPartialTime();
            return new TomlLocalTime(MakeTime(hour, minute, second, fractionTicks));
        }

        /// <summary>
        /// Reads the <c>HH:MM[:SS[.fraction]]</c> portion of a time value.
        /// </summary>
        /// <returns>The hour, minute, second, and fractional ticks.</returns>
        private (int Hour, int Minute, int Second, long FractionTicks) ReadPartialTime()
        {
            var hour = ReadDateDigits(2);
            ExpectDateChar(':');
            var minute = ReadDateDigits(2);

            var second = 0;
            long fractionTicks = 0;
            if (!Eof && Current == ':')
            {
                Advance();
                second = ReadDateDigits(2);
                if (!Eof && Current == '.')
                {
                    Advance();
                    fractionTicks = ReadFractionTicks();
                }
            }
            else if (_specVersion != TomlSpecVersion.V1_1)
            {
                // Optional seconds were introduced in TOML v1.1.0; v1.0 requires HH:MM:SS.
                throw Error(FormatsResourceStrings.Format_Invalid_TomlSecondsRequired);
            }

            return (hour, minute, second, fractionTicks);
        }

        /// <summary>
        /// Reads the fractional-seconds digits and converts them to ticks, truncating beyond 100-nanosecond precision.
        /// </summary>
        /// <returns>The fractional-second value in ticks.</returns>
        private long ReadFractionTicks()
        {
            var start = _pos;
            while (!Eof && IsDigit(Current))
                Advance();
            if (_pos == start)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidDateTime);

            var digits = _src.AsSpan(start, _pos - start);
            long ticks = 0;
            for (var i = 0; i < 7; i++)
                ticks = (ticks * 10) + (i < digits.Length ? digits[i] - '0' : 0);
            return ticks;
        }

        /// <summary>
        /// Reads a time-zone offset (<c>Z</c> or <c>±HH:MM</c>).
        /// </summary>
        /// <returns>The offset.</returns>
        private TimeSpan ReadTimeOffset()
        {
            if (Current == 'Z' || Current == 'z')
            {
                Advance();
                return TimeSpan.Zero;
            }

            var negative = Current == '-';
            Advance();
            var hours = ReadDateDigits(2);
            ExpectDateChar(':');
            var minutes = ReadDateDigits(2);
            if (hours > 23 || minutes > 59)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidDateTime);

            var offset = new TimeSpan(hours, minutes, 0);
            return negative ? -offset : offset;
        }

        /// <summary>
        /// Reads exactly <paramref name="count" /> decimal digits as part of a date-time value.
        /// </summary>
        /// <param name="count">The digit count.</param>
        /// <returns>The parsed integer.</returns>
        private int ReadDateDigits(int count)
        {
            if (_pos + count > _src.Length)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidDateTime);

            var value = 0;
            for (var i = 0; i < count; i++)
            {
                var c = _src[_pos + i];
                if (!IsDigit(c))
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidDateTime);
                value = (value * 10) + (c - '0');
            }

            _pos += count;
            return value;
        }

        /// <summary>
        /// Consumes the expected separator character within a date-time value.
        /// </summary>
        /// <param name="expected">The expected character.</param>
        private void ExpectDateChar(char expected)
        {
            if (Eof || Current != expected)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidDateTime);
            Advance();
        }

        /// <summary>
        /// Builds a <see cref="DateOnly" /> from validated components.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <returns>The date.</returns>
        private DateOnly MakeDate(int year, int month, int day)
        {
            try
            {
                return new DateOnly(year, month, day);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidDateTime);
            }
        }

        /// <summary>
        /// Builds a <see cref="TimeOnly" /> from validated components.
        /// </summary>
        /// <param name="hour">The hour.</param>
        /// <param name="minute">The minute.</param>
        /// <param name="second">The second.</param>
        /// <param name="fractionTicks">The fractional-second ticks.</param>
        /// <returns>The time.</returns>
        private TimeOnly MakeTime(int hour, int minute, int second, long fractionTicks)
        {
            // Leap seconds (:60) cannot be represented by TimeOnly/DateTime; reject rather than silently clamp to :59.
            if (second == 60)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlLeapSecond);
            if (hour > 23 || minute > 59 || second > 59)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidDateTime);

            var ticks = (((hour * 3600L) + (minute * 60L) + second) * TicksPerSecond) + fractionTicks;
            return new TimeOnly(ticks);
        }

        /// <summary>
        /// Builds a <see cref="DateTime" /> with <see cref="DateTimeKind.Unspecified" /> from validated components.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <param name="hour">The hour.</param>
        /// <param name="minute">The minute.</param>
        /// <param name="second">The second.</param>
        /// <param name="fractionTicks">The fractional-second ticks.</param>
        /// <returns>The date-time.</returns>
        private DateTime MakeDateTime(int year, int month, int day, int hour, int minute, int second, long fractionTicks)
        {
            // Leap seconds (:60) cannot be represented by TimeOnly/DateTime; reject rather than silently clamp to :59.
            if (second == 60)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlLeapSecond);
            if (hour > 23 || minute > 59 || second > 59)
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidDateTime);

            try
            {
                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified).AddTicks(fractionTicks);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw Error(FormatsResourceStrings.Format_Invalid_TomlInvalidDateTime);
            }
        }

        /// <summary>
        /// Indicates whether <paramref name="c" /> is an ASCII decimal digit.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns><see langword="true" /> when the character is <c>0-9</c>.</returns>
        private static bool IsDigit(char c) =>
            c is >= '0' and <= '9';

        /// <summary>
        /// Indicates whether every character of <paramref name="s" /> is an ASCII decimal digit.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns><see langword="true" /> when all characters are digits.</returns>
        private static bool AllAsciiDigits(string s)
        {
            foreach (var c in s)
            {
                if (!IsDigit(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Removes underscores from a numeric body, requiring each underscore to sit between two digits of the
        /// appropriate class.
        /// </summary>
        /// <param name="s">The numeric body.</param>
        /// <param name="allowHexLetters">
        /// When <see langword="true" />, hexadecimal letters count as adjacent digits; otherwise only decimal digits
        /// do.
        /// </param>
        /// <param name="result">The body with underscores removed.</param>
        /// <returns><see langword="true" /> when underscore placement is valid.</returns>
        private static bool TryStripUnderscores(string s, bool allowHexLetters, out string result)
        {
            result = string.Empty;
            if (s.IndexOf('_') < 0)
            {
                result = s;
                return true;
            }

            var sb = new StringBuilder(s.Length);
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] == '_')
                {
                    if (i == 0 || i == s.Length - 1 || !IsNeighborDigit(s[i - 1], allowHexLetters) || !IsNeighborDigit(s[i + 1], allowHexLetters))
                        return false;
                }
                else
                {
                    sb.Append(s[i]);
                }
            }

            result = sb.ToString();
            return true;
        }

        /// <summary>
        /// Indicates whether <paramref name="c" /> may sit adjacent to an underscore in a numeric literal of the given
        /// class.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <param name="allowHexLetters">When <see langword="true" />, hexadecimal letters are permitted.</param>
        /// <returns><see langword="true" /> when the character is a permitted neighbor digit.</returns>
        private static bool IsNeighborDigit(char c, bool allowHexLetters) =>
            allowHexLetters ? IsHexLike(c) : IsDigit(c);

        /// <summary>
        /// Indicates whether <paramref name="c" /> may sit adjacent to an underscore in a numeric literal.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns><see langword="true" /> when the character is an ASCII alphanumeric digit.</returns>
        private static bool IsHexLike(char c) =>
            IsDigit(c) || (c is >= 'a' and <= 'f') || (c is >= 'A' and <= 'F');

        /// <summary>
        /// Defines a standard table at <paramref name="path" /> and makes it the current table.
        /// </summary>
        /// <param name="path">The table key path.</param>
        private void DefineStandardTable(List<string> path)
        {
            var table = _root;
            for (var i = 0; i < path.Count - 1; i++)
                table = WalkHeaderSegment(table, path[i]);

            var key = path[^1];
            if (table.TryGetValue(key, out var existing))
            {
                if (existing is not TomlTable child)
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlDuplicateTable);
                if (_inline.Contains(child))
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlExtendInlineTable);
                if (_headerDefined.Contains(child) || _dotted.Contains(child))
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlDuplicateTable);

                _implicitSuper.Remove(child);
                _headerDefined.Add(child);
                _current = child;
                return;
            }

            var created = new TomlTable();
            table[key] = created;
            _headerDefined.Add(created);
            _current = created;
        }

        /// <summary>
        /// Defines (or appends to) an array of tables at <paramref name="path" /> and makes the new element the current
        /// table.
        /// </summary>
        /// <param name="path">The array key path.</param>
        private void DefineArrayTable(List<string> path)
        {
            var table = _root;
            for (var i = 0; i < path.Count - 1; i++)
                table = WalkHeaderSegment(table, path[i]);

            var key = path[^1];
            TomlArray array;
            if (table.TryGetValue(key, out var existing))
            {
                if (existing is not TomlArray found || !_tableArrays.Contains(found))
                {
                    throw Error(existing is TomlArray
                        ? FormatsResourceStrings.Format_Invalid_TomlAppendToStaticArray
                        : FormatsResourceStrings.Format_Invalid_TomlArrayTableConflict);
                }

                array = found;
            }
            else
            {
                array = new TomlArray();
                _tableArrays.Add(array);
                table[key] = array;
            }

            var element = new TomlTable();
            array.Add(element);
            _current = element;
        }

        /// <summary>
        /// Walks (creating if necessary) a single super-table segment of a table header path.
        /// </summary>
        /// <param name="table">The table to walk from.</param>
        /// <param name="key">The segment key.</param>
        /// <returns>The table for the segment.</returns>
        private TomlTable WalkHeaderSegment(TomlTable table, string key)
        {
            if (_inline.Contains(table))
                throw Error(FormatsResourceStrings.Format_Invalid_TomlExtendInlineTable);

            if (table.TryGetValue(key, out var existing))
            {
                switch (existing)
                {
                    case TomlTable child when !_inline.Contains(child):
                        return child;
                    case TomlTable:
                        throw Error(FormatsResourceStrings.Format_Invalid_TomlExtendInlineTable);
                    case TomlArray array when _tableArrays.Contains(array) && array.Count > 0:
                        return (TomlTable)array[array.Count - 1];
                    default:
                        throw Error(FormatsResourceStrings.Format_Invalid_TomlDuplicateTable);
                }
            }

            var created = new TomlTable();
            table[key] = created;
            _implicitSuper.Add(created);
            return created;
        }

        /// <summary>
        /// Assigns a key/value pair under <paramref name="target" />, creating intermediate dotted-key tables.
        /// </summary>
        /// <param name="target">The table receiving the assignment.</param>
        /// <param name="path">The dotted key path.</param>
        /// <param name="value">The value to assign.</param>
        private void AssignKeyValue(TomlTable target, List<string> path, TomlValue value)
        {
            var table = target;
            for (var i = 0; i < path.Count - 1; i++)
                table = WalkDottedSegment(table, path[i]);

            var key = path[^1];
            if (table.ContainsKey(key))
                throw Error(FormatsResourceStrings.Format_Invalid_TomlDuplicateKey);

            table[key] = value;
        }

        /// <summary>
        /// Walks (creating if necessary) a single intermediate table segment of a dotted key.
        /// </summary>
        /// <param name="table">The table to walk from.</param>
        /// <param name="key">The segment key.</param>
        /// <returns>The intermediate table.</returns>
        private TomlTable WalkDottedSegment(TomlTable table, string key)
        {
            if (table.TryGetValue(key, out var existing))
            {
                if (existing is not TomlTable child)
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlKeyOnValue);
                if (_inline.Contains(child))
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlExtendInlineTable);

                // Dotted keys may not extend a table already defined in [table] form (see toml-lang/toml#846).
                if (_headerDefined.Contains(child))
                    throw Error(FormatsResourceStrings.Format_Invalid_TomlDuplicateTable);

                return child;
            }

            var created = new TomlTable();
            _dotted.Add(created);
            table[key] = created;
            return created;
        }

        /// <summary>
        /// Creates a <see cref="TomlFormatException" /> tagged with the current source line.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The exception to throw.</returns>
        private TomlFormatException Error(string message) =>
            new(message, _line);
    }
}
