// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Delimited.Parser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using Bodu.Text.Formats;

namespace Bodu.Text.Delimited;

public static partial class Delimited
{
    private static readonly CompositeFormat s_unterminatedQuotedField =
        CompositeFormat.Parse(FormatsResourceStrings.Format_Invalid_DelimitedUnterminatedQuotedField);

    private static readonly CompositeFormat s_duplicateHeader =
        CompositeFormat.Parse(FormatsResourceStrings.Format_Invalid_DelimitedDuplicateHeader);

    private static readonly CompositeFormat s_fieldCountMismatch =
        CompositeFormat.Parse(FormatsResourceStrings.Format_Invalid_DelimitedFieldCountMismatch);

    private static readonly CompositeFormat s_malformedRecord =
        CompositeFormat.Parse(FormatsResourceStrings.Format_Invalid_DelimitedMalformedRecord);

    /// <summary>
    /// Throws a <see cref="DelimitedFormatException" /> for an unterminated quoted field.
    /// </summary>
    /// <param name="lineNumber">The 1-based line on which the opening quote appeared.</param>
    [DoesNotReturn]
    internal static void ThrowUnterminatedQuotedField(int lineNumber) =>
        throw new DelimitedFormatException(
            string.Format(CultureInfo.CurrentCulture, s_unterminatedQuotedField, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DelimitedFormatException" /> for a duplicated header name.
    /// </summary>
    /// <param name="headerName">The header name that appears more than once.</param>
    /// <param name="lineNumber">The 1-based line on which the header row appears.</param>
    [DoesNotReturn]
    internal static void ThrowDuplicateHeader(string headerName, int lineNumber) =>
        throw new DelimitedFormatException(
            string.Format(CultureInfo.CurrentCulture, s_duplicateHeader, headerName, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DelimitedFormatException" /> for a character that appears after the closing quote of a
    /// quoted field but before the next delimiter or line terminator.
    /// </summary>
    /// <param name="unexpected">The offending character.</param>
    /// <param name="lineNumber">The 1-based line on which the malformed record was detected.</param>
    [DoesNotReturn]
    internal static void ThrowMalformedRecord(char unexpected, int lineNumber) =>
        throw new DelimitedFormatException(
            string.Format(CultureInfo.CurrentCulture, s_malformedRecord, unexpected, lineNumber), lineNumber);

    /// <summary>
    /// Throws a <see cref="DelimitedFormatException" /> when a data row's field count does not match the header.
    /// </summary>
    /// <param name="rowLineNumber">The 1-based line on which the data row appears.</param>
    /// <param name="rowFieldCount">The actual field count of the offending row.</param>
    /// <param name="headerFieldCount">The field count established by the header row.</param>
    [DoesNotReturn]
    internal static void ThrowFieldCountMismatch(int rowLineNumber, int rowFieldCount, int headerFieldCount) =>
        throw new DelimitedFormatException(
            string.Format(CultureInfo.CurrentCulture, s_fieldCountMismatch, rowLineNumber, rowFieldCount, headerFieldCount),
            rowLineNumber);

    /// <summary>
    /// Builds the column name-to-index map for a header row according to the configured duplicate-header policy.
    /// </summary>
    /// <param name="headers">The parsed header field values in source order.</param>
    /// <param name="behavior">The duplicate-header resolution policy.</param>
    /// <param name="lineNumber">The 1-based line on which the header row appears.</param>
    /// <returns>
    /// A read-only map from header name to the column index assigned by <paramref name="behavior" />.
    /// </returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when <paramref name="behavior" /> is <see cref="DelimitedDuplicateHeaderBehavior.Throw" /> and any header
    /// name appears more than once.
    /// </exception>
    private static IReadOnlyDictionary<string, int> BuildHeaderIndex(
        List<string> headers,
        DelimitedDuplicateHeaderBehavior behavior,
        int lineNumber)
    {
        // First pass: identify duplicate header names so AllowDuplicates can omit them and Throw can fail fast.
        HashSet<string>? duplicates = null;
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (var i = 0; i < headers.Count; i++)
        {
            if (!seen.Add(headers[i]))
            {
                if (behavior == DelimitedDuplicateHeaderBehavior.Throw)
                    ThrowDuplicateHeader(headers[i], lineNumber);

                duplicates ??= new HashSet<string>(StringComparer.Ordinal);
                duplicates.Add(headers[i]);
            }
        }

        Dictionary<string, int> map = new(StringComparer.Ordinal);

        switch (behavior)
        {
            case DelimitedDuplicateHeaderBehavior.FirstWins:
                for (var i = 0; i < headers.Count; i++)
                    _ = map.TryAdd(headers[i], i);
                break;

            case DelimitedDuplicateHeaderBehavior.AllowDuplicates:
                for (var i = 0; i < headers.Count; i++)
                {
                    if (duplicates is null || !duplicates.Contains(headers[i]))
                        map[headers[i]] = i;
                }

                break;

            case DelimitedDuplicateHeaderBehavior.LastWins:
            default:
                for (var i = 0; i < headers.Count; i++)
                    map[headers[i]] = i;
                break;
        }

        return map;
    }

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
                var recordLineNumber = _lineNumber;
                List<string> fields = ParseRecord(delimiter, quote);

                if (firstRecord && _options.HasHeader)
                {
                    // First record is the header row.
                    headers = fields;
                    roHeaderIndex = BuildHeaderIndex(headers, _options.DuplicateHeaderBehavior, recordLineNumber);
                    firstRecord = false;
                    continue;
                }

                if (headers is not null
                    && _options.FieldCountBehavior == DelimitedFieldCountBehavior.Strict
                    && fields.Count != headers.Count)
                {
                    ThrowFieldCountMismatch(recordLineNumber, fields.Count, headers.Count);
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

                // Unexpected character after a quoted field. Throw or skip according to the configured policy.
                if (_options.MalformedRecordBehavior == DelimitedMalformedRecordBehavior.Throw)
                    ThrowMalformedRecord(Current, _lineNumber);

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
        private void Advance() => LineScanner.Advance(ref _remaining, ref _lineNumber);

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
