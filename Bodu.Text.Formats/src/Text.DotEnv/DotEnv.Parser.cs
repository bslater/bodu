// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnv.Parser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using Bodu.Text.Formats;

namespace Bodu.Text.DotEnv;

public static partial class DotEnv
{
    /// <summary>
    /// Throws a <see cref="DotEnvFormatException" /> for a duplicate key.
    /// </summary>
    /// <param name="key">The duplicate key name.</param>
    /// <param name="lineNumber">The 1-based line number where the duplicate key was found.</param>
    [DoesNotReturn]
    private static void ThrowDuplicateKey(string key, int lineNumber) =>
        throw new DotEnvFormatException(
            string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Format_Invalid_DotEnvDuplicateKey, key, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DotEnvFormatException" /> for an invalid key name.
    /// </summary>
    /// <param name="key">The offending key text.</param>
    /// <param name="lineNumber">The 1-based line number where the invalid key was found.</param>
    [DoesNotReturn]
    internal static void ThrowInvalidKey(string key, int lineNumber) =>
        throw new DotEnvFormatException(
            string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Format_Invalid_DotEnvInvalidKey, key, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DotEnvFormatException" /> for a malformed entry line.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number of the malformed entry.</param>
    [DoesNotReturn]
    internal static void ThrowMalformedEntry(int lineNumber) =>
        throw new DotEnvFormatException(
            string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Format_Invalid_DotEnvMalformedEntry, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DotEnvFormatException" /> for an unterminated double-quoted string.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number on which the unterminated value begins.</param>
    [DoesNotReturn]
    internal static void ThrowUnterminatedDoubleQuote(int lineNumber) =>
        throw new DotEnvFormatException(
            string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Format_Invalid_DotEnvUnterminatedDoubleQuote, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DotEnvFormatException" /> for an unterminated single-quoted string.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number on which the unterminated value begins.</param>
    [DoesNotReturn]
    internal static void ThrowUnterminatedSingleQuote(int lineNumber) =>
        throw new DotEnvFormatException(
            string.Format(CultureInfo.CurrentCulture, FormatsResourceStrings.Format_Invalid_DotEnvUnterminatedSingleQuote, lineNumber), lineNumber);

    /// <summary>
    /// Provides character-by-character DotEnv parsing over a <see cref="ReadOnlySpan{T}" /> of characters.
    /// </summary>
    private ref struct Parser
    {
        private readonly DotEnvParseOptions _options;
        private ReadOnlySpan<char> _remaining;
        private int _lineNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser" /> struct.
        /// </summary>
        /// <param name="source">The full DotEnv source text.</param>
        /// <param name="options">Options that control parsing behaviour.</param>
        public Parser(ReadOnlySpan<char> source, DotEnvParseOptions options)
        {
            _remaining = source;
            _options = options;
            _lineNumber = 1;
        }

        /// <summary>
        /// Gets a value indicating whether the remaining source is exhausted.
        /// </summary>
        private readonly bool IsEmpty => _remaining.IsEmpty;

        /// <summary>
        /// Gets the current character without consuming it, or <c>'\0'</c> when exhausted.
        /// </summary>
        private readonly char Current => _remaining.IsEmpty ? '\0' : _remaining[0];

        /// <summary>
        /// Parses the source text and returns a fully constructed <see cref="DotEnvDocument" />.
        /// </summary>
        /// <returns>The parsed document.</returns>
        /// <exception cref="DotEnvFormatException">Thrown when the source is malformed.</exception>
        public DotEnvDocument Parse()
        {
            List<DotEnvEntry> entries = new();
            Dictionary<string, DotEnvEntry> lookup = new(StringComparer.Ordinal);
            List<DotEnvComment>? pendingComments = null;

            while (!IsEmpty)
            {
                SkipWhitespaceOnLine();

                if (IsEmpty)
                    break;

                var c = Current;

                if (c is '\r' or '\n')
                {
                    SkipLineEnding();
                    continue;
                }

                if (c == '#')
                {
                    var commentLine = _lineNumber;
                    _remaining = _remaining[1..]; // consume '#'
                    var len = 0;
                    while (len < _remaining.Length && _remaining[len] != '\n' && _remaining[len] != '\r')
                        len++;

                    if (_options.PreserveComments)
                    {
                        pendingComments ??= new List<DotEnvComment>();
                        pendingComments.Add(new DotEnvComment('#', new string(_remaining[..len]), commentLine));
                    }

                    _remaining = _remaining[len..];
                    SkipLineEnding();
                    continue;
                }

                var entryLineNumber = _lineNumber;

                // Optionally strip "export " prefix (word "export" followed by one or more spaces/tabs).
                if (_options.AllowExportPrefix &&
                    _remaining.Length > 6 &&
                    _remaining[..6].SequenceEqual("export".AsSpan()) &&
                    (_remaining[6] == ' ' || _remaining[6] == '\t'))
                {
                    _remaining = _remaining[6..];
                    SkipWhitespaceOnLine();
                }

                // Parse key: [A-Za-z_][A-Za-z0-9_]*
                if (IsEmpty || !IsKeyStart(Current))
                {
                    var badToken = IsEmpty ? string.Empty : Current.ToString();
                    DotEnv.ThrowInvalidKey(badToken, entryLineNumber);
                }

                var keyLen = 1;
                while (keyLen < _remaining.Length && IsKeyContinue(_remaining[keyLen]))
                    keyLen++;

                var key = new string(_remaining[..keyLen]);
                _remaining = _remaining[keyLen..];

                // Expect '='
                if (IsEmpty || Current != '=')
                    DotEnv.ThrowMalformedEntry(entryLineNumber);

                _remaining = _remaining[1..];

                // Dispatch value parsing by leading delimiter.
                var value = Current == '"'
                    ? ParseDoubleQuotedValue(entryLineNumber)
                    : Current == '\'' ? ParseSingleQuotedValue(entryLineNumber) : ParseUnquotedValue();

                IReadOnlyList<DotEnvComment> leadingComments = pendingComments is null
                    ? Array.Empty<DotEnvComment>()
                    : pendingComments.AsReadOnly();
                pendingComments = null;

                // Apply duplicate key behavior.
                if (lookup.TryGetValue(key, out DotEnvEntry? existing))
                {
                    switch (_options.DuplicateKeyBehavior)
                    {
                        case DuplicateKeyPolicy.Disallowed:
                            DotEnv.ThrowDuplicateKey(key, entryLineNumber);
                            return default!;

                        case DuplicateKeyPolicy.FirstWins:
                            break;

                        case DuplicateKeyPolicy.LastWins:
                            DotEnvEntry replacement = new(key, value, leadingComments);
                            var idx = entries.IndexOf(existing);
                            entries[idx] = replacement;
                            lookup[key] = replacement;
                            break;
                    }
                }
                else
                {
                    DotEnvEntry entry = new(key, value, leadingComments);
                    entries.Add(entry);
                    lookup[key] = entry;
                }

                SkipToEndOfLine();
                SkipLineEnding();
            }

            return new DotEnvDocument(entries, lookup);
        }

        /// <summary>
        /// Returns <see langword="true" /> when <paramref name="c" /> is a valid key start character.
        /// </summary>
        /// <param name="c">The character to test.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="c" /> is an ASCII letter or underscore; otherwise,
        /// <see langword="false" />.
        /// </returns>
        private static bool IsKeyStart(char c) =>
            c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';

        /// <summary>
        /// Returns <see langword="true" /> when <paramref name="c" /> is a valid key continuation character.
        /// </summary>
        /// <param name="c">The character to test.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="c" /> is a key start character or an ASCII digit; otherwise,
        /// <see langword="false" />.
        /// </returns>
        private static bool IsKeyContinue(char c) =>
            IsKeyStart(c) || (c >= '0' && c <= '9');

        /// <summary>
        /// Parses a double-quoted value starting at the current position, resolving escape sequences and allowing
        /// literal embedded newlines.
        /// </summary>
        /// <param name="startLineNumber">The 1-based line number on which the opening <c>"</c> appears.</param>
        /// <returns>The fully processed string value, with quotes stripped and escape sequences resolved.</returns>
        private string ParseDoubleQuotedValue(int startLineNumber)
        {
            Advance(); // consume opening '"'

            StringBuilder sb = new();

            while (true)
            {
                if (IsEmpty)
                    DotEnv.ThrowUnterminatedDoubleQuote(startLineNumber);

                var c = Current;

                if (c == '"')
                {
                    Advance(); // consume closing '"'
                    return sb.ToString();
                }

                if (c == '\\')
                {
                    Advance(); // consume '\'

                    if (IsEmpty)
                        DotEnv.ThrowUnterminatedDoubleQuote(startLineNumber);

                    var esc = Current;
                    Advance(); // consume escape char (may be '\n', incrementing _lineNumber)

                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case '$': sb.Append('$'); break;
                        case '\n': break; // line continuation — discard backslash and newline
                        case '\r':
                            // \<CR><LF> continuation — LF was not yet consumed.
                            if (!IsEmpty && Current == '\n')
                                Advance();
                            break;
                        default:
                            sb.Append('\\');
                            sb.Append(esc);
                            break;
                    }

                    continue;
                }

                // Literal character, including embedded newlines.
                sb.Append(c);
                Advance();
            }
        }

        /// <summary>
        /// Parses a single-quoted value starting at the current position. The content is literal — no escape processing
        /// — and the value must be closed on the same line.
        /// </summary>
        /// <param name="startLineNumber">The 1-based line number on which the opening <c>'</c> appears.</param>
        /// <returns>The literal string value with surrounding quotes removed.</returns>
        private string ParseSingleQuotedValue(int startLineNumber)
        {
            _remaining = _remaining[1..]; // consume opening '\''

            // Locate the closing quote, ensuring it appears before any line break.
            for (var i = 0; i < _remaining.Length; i++)
            {
                var c = _remaining[i];

                if (c == '\'')
                {
                    var value = new string(_remaining[..i]);
                    _remaining = _remaining[(i + 1)..];
                    return value;
                }

                if (c is '\n' or '\r')
                    DotEnv.ThrowUnterminatedSingleQuote(startLineNumber);
            }

            DotEnv.ThrowUnterminatedSingleQuote(startLineNumber);
            return default!;
        }

        /// <summary>
        /// Parses an unquoted value starting at the current position. Leading whitespace is trimmed, and — when
        /// <see cref="DotEnvParseOptions.AllowInlineComments" /> is enabled — a <c>#</c> preceded by whitespace
        /// terminates the value.
        /// </summary>
        /// <returns>The trimmed value string.</returns>
        private string ParseUnquotedValue()
        {
            var len = 0;

            while (len < _remaining.Length && _remaining[len] != '\n' && _remaining[len] != '\r')
                len++;

            ReadOnlySpan<char> raw = _remaining[..len];
            _remaining = _remaining[len..];

            raw = raw.TrimStart();

            if (_options.AllowInlineComments)
            {
                for (var i = 1; i < raw.Length; i++)
                {
                    if (raw[i] == '#' && (raw[i - 1] == ' ' || raw[i - 1] == '\t'))
                    {
                        raw = raw[..i];
                        break;
                    }
                }
            }

            return new string(raw.TrimEnd());
        }

        /// <summary>
        /// Consumes one character, incrementing <see cref="_lineNumber" /> when the consumed character is <c>'\n'</c>.
        /// </summary>
        private void Advance() => LineScanner.Advance(ref _remaining, ref _lineNumber);

        /// <summary>
        /// Advances past any space or tab characters on the current line without consuming newline characters.
        /// </summary>
        private void SkipWhitespaceOnLine()
        {
            while (!_remaining.IsEmpty && (_remaining[0] == ' ' || _remaining[0] == '\t'))
                _remaining = _remaining[1..];
        }

        /// <summary>
        /// Advances to the end of the current line without consuming the line terminator.
        /// </summary>
        private void SkipToEndOfLine() => LineScanner.SkipToEndOfLine(ref _remaining);

        /// <summary>
        /// Consumes a <c>\r\n</c>, <c>\r</c>, or <c>\n</c> line terminator and increments <see cref="_lineNumber" />
        /// accordingly.
        /// </summary>
        private void SkipLineEnding() => LineScanner.SkipLineEnding(ref _remaining, ref _lineNumber);
    }
}
