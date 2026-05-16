// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedKnownAnswerVectors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Provides Known Answer Test vectors for the RFC 4180 delimited-text format. Each provider method yields rows
/// in the shape expected by MSTest's <c>[DynamicData]</c> attribute.
/// </summary>
public static class DelimitedKnownAnswerVectors
{
    /// <summary>
    /// RFC 4180, §2 citation source.
    /// </summary>
    private const string Rfc4180 = "RFC 4180, §2";

    /// <summary>
    /// Returns the concatenation of all vector provider methods, suitable for exhaustive round-trip tests.
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> AllVectors()
    {
        foreach (object[] row in Rfc4180Vectors())
            yield return row;
    }

    /// <summary>
    /// Returns vectors derived from RFC 4180 §2, covering each normative rule and widely-adopted dialect
    /// extensions (LF-only line endings, TSV, bare CR, Unicode content).
    /// </summary>
    /// <returns>A sequence suitable for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> Rfc4180Vectors()
    {
        // §2, rule 1: CRLF is the canonical record separator.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "CRLF line endings",
                "a,b\r\n1,2",
                ["a", "b"],
                [["1", "2"]],
                Source: Rfc4180),
        };

        // De facto standard: LF-only line endings are universally accepted.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "LF-only line endings",
                "a,b\n1,2",
                ["a", "b"],
                [["1", "2"]],
                Source: Rfc4180),
        };

        // §2, rule 3: header row is optional; with HasHeader=false every record is a data row.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "No header row",
                "1,2\r\n3,4",
                [],
                [["1", "2"], ["3", "4"]],
                new DelimitedParseOptions { HasHeader = false },
                Source: Rfc4180),
        };

        // §2, rule 5: fields containing the delimiter must be enclosed in double quotes.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Quoted field containing delimiter",
                "city\r\n\"New York, NY\"",
                ["city"],
                [["New York, NY"]],
                Source: Rfc4180),
        };

        // §2, rule 7: a double-quote inside a quoted field is represented by two consecutive double-quote characters.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Doubled quote inside quoted field",
                "q\r\n\"say \"\"hi\"\"\"",
                ["q"],
                [["say \"hi\""]],
                Source: Rfc4180),
        };

        // §2, rule 6: fields enclosed in double quotes may contain line breaks.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Quoted field with embedded CRLF",
                "note\r\n\"line1\r\nline2\"",
                ["note"],
                [["line1\r\nline2"]],
                Source: Rfc4180),
        };

        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Quoted field with embedded LF",
                "note\r\n\"line1\nline2\"",
                ["note"],
                [["line1\nline2"]],
                Source: Rfc4180),
        };

        // Consecutive delimiters produce empty unquoted fields.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Empty unquoted fields (consecutive commas)",
                "a,b,c\r\n1,,3",
                ["a", "b", "c"],
                [["1", "", "3"]],
                Source: Rfc4180),
        };

        // An empty quoted field ("") yields an empty string, not a null.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Empty quoted field",
                "a\r\n\"\"",
                ["a"],
                [[""]],
                Source: Rfc4180),
        };

        // Leading and trailing positions may be empty.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Leading and trailing empty fields",
                "a,b,c\r\n,2,",
                ["a", "b", "c"],
                [["", "2", ""]],
                Source: Rfc4180),
        };

        // §2, rule 2: the final record may or may not be followed by a line break.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Last record without trailing line break",
                "a,b\r\n1,2",
                ["a", "b"],
                [["1", "2"]],
                Source: Rfc4180),
        };

        // §2, example 1: canonical headerless example from the specification body.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "RFC 4180 canonical example without header",
                "\"aaa\",\"bbb\",\"ccc\"\r\nzzz,yyy,xxx",
                [],
                [["aaa", "bbb", "ccc"], ["zzz", "yyy", "xxx"]],
                new DelimitedParseOptions { HasHeader = false },
                Source: Rfc4180),
        };

        // §2, example 2: canonical example with an explicit header record.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "RFC 4180 canonical example with header",
                "field_a,field_b,field_c\r\n\"aaa\",\"bbb\",\"ccc\"\r\nzzz,yyy,xxx",
                ["field_a", "field_b", "field_c"],
                [["aaa", "bbb", "ccc"], ["zzz", "yyy", "xxx"]],
                Source: Rfc4180),
        };

        // Every field may be individually quoted even when quoting is not required.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "All fields quoted",
                "\"a\",\"b\"\r\n\"1\",\"2\"",
                ["a", "b"],
                [["1", "2"]],
                Source: Rfc4180),
        };

        // §2, rule 4: spaces are considered part of a field and must not be ignored.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Whitespace in unquoted field preserved",
                "a\r\n  hello  ",
                ["a"],
                [["  hello  "]],
                Source: Rfc4180),
        };

        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Whitespace in quoted field preserved",
                "a\r\n\"  hello  \"",
                ["a"],
                [["  hello  "]],
                Source: Rfc4180),
        };

        // Single-column, single-row — minimal well-formed document with a header.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Single column single row",
                "name\r\nAlice",
                ["name"],
                [["Alice"]],
                Source: Rfc4180),
        };

        // Multiple data rows with mixed content.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Three rows",
                "name,age\r\nAlice,30\r\nBob,25\r\nCarol,35",
                ["name", "age"],
                [["Alice", "30"], ["Bob", "25"], ["Carol", "35"]],
                Source: Rfc4180),
        };

        // Tab-separated values (TSV) via configurable delimiter.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Tab-separated values (TSV)",
                "a\tb\n1\t2",
                ["a", "b"],
                [["1", "2"]],
                new DelimitedParseOptions { Delimiter = '\t' },
                Source: Rfc4180),
        };

        // Non-ASCII content: the parser must preserve Unicode characters verbatim.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Unicode content",
                "greeting\r\nhéllo",
                ["greeting"],
                [["héllo"]],
                Source: Rfc4180),
        };

        // Bare CR (\r without a following \n) treated as a record separator.
        yield return new object[]
        {
            new DelimitedKnownAnswerVector(
                "Bare CR line ending",
                "a,b\r1,2",
                ["a", "b"],
                [["1", "2"]],
                Source: Rfc4180),
        };
    }
}
