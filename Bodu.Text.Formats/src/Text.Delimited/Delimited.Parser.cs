// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Delimited.Parser.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Bodu.Text.Delimited;

public static partial class Delimited
{
    private static readonly CompositeFormat s_unterminatedQuotedField =
        CompositeFormat.Parse(FormatsResourceStrings.Format_Invalid_DelimitedUnterminatedQuotedField);

    /// <summary>
    /// Throws a <see cref="DelimitedFormatException" /> for an unterminated quoted field.
    /// </summary>
    /// <param name="lineNumber">The 1-based line on which the opening quote appeared.</param>
    [DoesNotReturn]
    internal static void ThrowUnterminatedQuotedField(int lineNumber) =>
        throw new DelimitedFormatException(
            string.Format(CultureInfo.InvariantCulture, s_unterminatedQuotedField, lineNumber), lineNumber);

    /// <summary>
    /// Provides RFC 4180-style character-by-character parsing over a <see cref="ReadOnlySpan{T}" /> of characters.
    /// </summary>
    private ref struct Parser
    {
        private readonly DelimitedParseOptions _options;
        private ReadOnlySpan<char> _remaining;
        private int _lineNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser" /> struct.
        /// </summary>
        /// <param name="source">The full delimited source text.</param>
        /// <param name="options">Options that control parsing behaviour.</param>
        public Parser(ReadOnlySpan<char> source, DelimitedParseOptions options)
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
        /// Parses the source text and returns a fully constructed <see cref="DelimitedDocument" />.
        /// </summary>
        /// <returns>The parsed document.</returns>
        /// <exception cref="DelimitedFormatException">Thrown when the source is structurally malformed.</exception>
        public DelimitedDocument Parse()
        {
            var delimiter = _options.Delimiter;
            var quote = _options.Quote;

            List<string>? headers = null;
            IReadOnlyDictionary<string, int>? roHeaderIndex = null;

            List<DelimitedRow> rows = new();
            var firstRecord = true;

            while (!IsEmpty)
            {
                // Skip blank lines.
                if (Current is '\r' or '\n')
                {
                    SkipLineEnding();
                    continue;
                }

                // Skip comment lines when AllowComments is enabled.
                if (_options.AllowComments && Current == _options.CommentChar)
                {
                    SkipToEndOfLine();
                    SkipLineEnding();
                    continue;
                }

                // Parse a full record (one or more fields separated by delimiter).
                List<string> fields = ParseRecord(delimiter, quote);

                if (firstRecord && _options.HasHeader)
                {
                    // First record is the header row.
                    headers = fields;
                    var headerIndex = new Dictionary<string, int>(StringComparer.Ordinal);

                    for (var i = 0; i < headers.Count; i++)
                        headerIndex[headers[i]] = i;

                    roHeaderIndex = headerIndex;
                    firstRecord = false;
                    continue;
                }

                firstRecord = false;
                rows.Add(new DelimitedRow(fields.AsReadOnly(), roHeaderIndex));
            }

            IReadOnlyList<string> headerList = headers is not null
                ? headers.AsReadOnly()
                : Array.Empty<string>();

            var fieldCount = headerList.Count > 0
                ? headerList.Count
                : (rows.Count > 0 ? rows[0].Count : 0);

            return new DelimitedDocument(headerList, rows.AsReadOnly(), fieldCount);
        }

        /// <summary>
        /// Parses a single record from the current position, consuming all fields up to and including the terminating
        /// line break (or end of source).
        /// </summary>
        /// <param name="delimiter">The field separator character.</param>
        /// <param name="quote">The quoting character.</param>
        /// <returns>A list of field values in source order.</returns>
        private List<string> ParseRecord(char delimiter, char quote)
        {
            List<string> fields = new();

            while (true)
            {
                var field = Current == quote
                    ? ParseQuotedField(quote)
                    : ParseUnquotedField(delimiter);

                if (_options.TrimFields && Current != quote)
                    field = field.Trim();

                fields.Add(field);

                if (IsEmpty || Current == '\r' || Current == '\n')
                {
                    SkipLineEnding();
                    break;
                }

                if (Current == delimiter)
                {
                    _remaining = _remaining[1..]; // consume delimiter
                    continue;
                }

                // Unexpected character after a quoted field — skip to end of record.
                SkipToEndOfLine();
                SkipLineEnding();
                break;
            }

            return fields;
        }

        /// <summary>
        /// Parses a quoted field starting at the current position. RFC 4180: two consecutive quote characters inside
        /// the field represent one literal quote. Literal newlines are preserved.
        /// </summary>
        /// <param name="quote">The quoting character.</param>
        /// <returns>The field content with surrounding quotes removed and doubled-quote escapes resolved.</returns>
        private string ParseQuotedField(char quote)
        {
            var startLine = _lineNumber;
            Advance(); // consume opening quote

            StringBuilder sb = new();

            while (true)
            {
                if (IsEmpty)
                    Delimited.ThrowUnterminatedQuotedField(startLine);

                var c = Current;

                if (c == quote)
                {
                    Advance(); // consume the quote character

                    // Two consecutive quotes → literal quote (RFC 4180 escape).
                    if (!IsEmpty && Current == quote)
                    {
                        sb.Append(quote);
                        Advance();
                        continue;
                    }

                    // Single quote followed by anything else → end of field.
                    return sb.ToString();
                }

                // Literal character, including embedded newlines.
                sb.Append(c);
                Advance();
            }
        }

        /// <summary>
        /// Parses an unquoted field starting at the current position, reading until the next delimiter, carriage
        /// return, line feed, or end of source.
        /// </summary>
        /// <param name="delimiter">The field separator character.</param>
        /// <returns>The raw field text.</returns>
        private string ParseUnquotedField(char delimiter)
        {
            var len = 0;

            while (len < _remaining.Length &&
                   _remaining[len] != delimiter &&
                   _remaining[len] != '\n' &&
                   _remaining[len] != '\r')
            {
                len++;
            }

            var field = new string(_remaining[..len]);
            _remaining = _remaining[len..];
            return field;
        }

        /// <summary>
        /// Consumes one character, incrementing <see cref="_lineNumber" /> when the consumed character is <c>'\n'</c>.
        /// </summary>
        private void Advance()
        {
            if (!_remaining.IsEmpty)
            {
                if (_remaining[0] == '\n')
                    _lineNumber++;

                _remaining = _remaining[1..];
            }
        }

        /// <summary>
        /// Advances to the end of the current line without consuming the line terminator.
        /// </summary>
        private void SkipToEndOfLine()
        {
            while (!_remaining.IsEmpty && _remaining[0] != '\n' && _remaining[0] != '\r')
                _remaining = _remaining[1..];
        }

        /// <summary>
        /// Consumes a <c>\r\n</c>, <c>\r</c>, or <c>\n</c> line terminator and increments <see cref="_lineNumber" />
        /// accordingly.
        /// </summary>
        private void SkipLineEnding()
        {
            if (_remaining.IsEmpty)
                return;

            if (_remaining[0] == '\r')
            {
                _remaining = _remaining[1..];

                if (!_remaining.IsEmpty && _remaining[0] == '\n')
                {
                    _lineNumber++;
                    _remaining = _remaining[1..];
                }
            }
            else if (_remaining[0] == '\n')
            {
                _lineNumber++;
                _remaining = _remaining[1..];
            }
        }
    }
}
