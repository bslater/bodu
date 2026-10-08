// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DelimitedReader.Read" />, verifying the token stream produced
/// for representative CSV inputs.
/// </summary>
[TestClass]
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Reads every token and returns a compact transcript.
    /// </summary>
    /// <param name="source">The CSV source text.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The token transcript.</returns>
    private static List<string> Transcribe(string source, DelimitedReaderOptions options = default)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        var reader = new Utf8DelimitedReader(bytes, options);
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                DelimitedTokenType.PropertyName => $"Name:{reader.GetString()}",
                DelimitedTokenType.String => $"String:{reader.GetString()}",
                _ => reader.TokenType.ToString(),
            });
        }

        return tokens;
    }

    /// <summary>
    /// Verifies that a header-mode CSV produces the object-framed token stream keyed by header name.
    /// </summary>
    [TestMethod]
    public void Read_WhenHeaderMode_ShouldFrameRecordsAsObjects()
    {
        List<string> tokens = Transcribe("name,age\nAda,36\n");

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartObject", "Name:name", "String:Ada", "Name:age", "String:36", "EndObject",
                "EndArray",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that headerless mode frames each record as a positional array.
    /// </summary>
    [TestMethod]
    public void Read_WhenHeaderless_ShouldFrameRecordsAsArrays()
    {
        List<string> tokens = Transcribe("Ada,36\nGrace,45\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartArray", "String:Ada", "String:36", "EndArray",
                "StartArray", "String:Grace", "String:45", "EndArray",
                "EndArray",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that a quoted field with an embedded delimiter, newline, and doubled quote decodes correctly.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldWithSpecials_ShouldDecode()
    {
        List<string> tokens = Transcribe("a\n\"x,y\r\nz\"\"q\"\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartArray", "String:a", "EndArray",
                "StartArray", "String:x,y\r\nz\"q", "EndArray",
                "EndArray",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that a record whose field count differs from the header throws under the strict default.
    /// </summary>
    [TestMethod]
    public void Read_WhenStrictFieldCountMismatch_ShouldThrowDelimitedFormatException()
    {
        Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("a,b\n1,2,3\n");
        });
    }

    /// <summary>
    /// Verifies that an unterminated quoted field throws <see cref="DelimitedFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnterminatedQuote_ShouldThrowDelimitedFormatException()
    {
        Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("a\n\"open\n", new DelimitedReaderOptions { NoHeader = true });
        });
    }

    /// <summary>
    /// Verifies that blank lines are skipped between records.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlankLines_ShouldSkipThem()
    {
        List<string> tokens = Transcribe("a\n\n1\n\n2\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartArray",
                "StartArray", "String:a", "EndArray",
                "StartArray", "String:1", "EndArray",
                "StartArray", "String:2", "EndArray",
                "EndArray",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that text after a closing quote on the second line is rejected with a
    /// <see cref="DelimitedFormatException" /> that carries line 2 and the offset of the byte after the closing quote.
    /// </summary>
    [TestMethod]
    public void Read_WhenTextFollowsAClosingQuoteOnLineTwo_ShouldReportLineTwo()
    {
        var ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("a,b\n\"rec1\"x,c\n", new DelimitedReaderOptions { NoHeader = true });
        });

        Assert.AreEqual(2, ex.LineNumber);
        Assert.AreEqual(10, ex.Offset);
    }

    /// <summary>
    /// Verifies that a record with fewer fields than the header is rejected with a
    /// <see cref="DelimitedFormatException" /> that carries the short record's line, 2, whether or not a line ending
    /// follows it.
    /// </summary>
    /// <param name="endsWithLineFeed">Whether a line feed follows the short record.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Read_WhenARecordIsShorterThanTheHeader_ShouldReportItsLine(bool endsWithLineFeed)
    {
        string source = "20322051544,1979,8.8017226E7,ABC,45,2000-01-01\n28392898392,1974,8.8392926E7,23,2050-11-27"
            + (endsWithLineFeed ? "\n" : string.Empty);

        var ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe(source);
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a quoted field that opens on line 2 and is still open when the input ends on line 4 is reported on
    /// line 4, the line at which the error was detected, since <see cref="DelimitedFormatException" /> carries a single
    /// line number.
    /// </summary>
    [TestMethod]
    public void Read_WhenAnUnterminatedQuoteSpansLines_ShouldReportTheLineWhereItWasDetected()
    {
        var ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("a,b\n\"d\n\n,e", new DelimitedReaderOptions { NoHeader = true });
        });

        Assert.AreEqual(4, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a line break inside an earlier quoted field counts towards the line of a later error, so an
    /// unterminated quote on the fourth line is reported on line 4.
    /// </summary>
    [TestMethod]
    public void Read_WhenAnErrorFollowsAQuotedLineBreak_ShouldCountTheQuotedLine()
    {
        var ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("a\n\"b\nc\"\n\"d", new DelimitedReaderOptions { NoHeader = true });
        });

        Assert.AreEqual(4, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a record of 8,191 fields, the first a run of 24,578 characters, followed by a longer record of
    /// 16,383 empty fields, reads as both records whole.
    /// </summary>
    [TestMethod]
    public void Read_WhenARecordOfManyFieldsIsFollowedByALongerOne_ShouldReadBoth()
    {
        string run = new('A', 24578);
        string source = run + new string(';', 8190) + "\n" + new string(';', 16382);

        List<string[]> records = ReadRecords(source, new DelimitedReaderOptions { Delimiter = ';', NoHeader = true });

        Assert.AreEqual(2, records.Count);
        Assert.AreEqual(8191, records[0].Length);
        Assert.AreEqual(run, records[0][0]);
        Assert.IsTrue(records[0].Skip(1).All(field => field.Length == 0), "The first record's later fields are not all empty.");
        Assert.AreEqual(16383, records[1].Length);
        Assert.IsTrue(records[1].All(field => field.Length == 0), "The second record's fields are not all empty.");
    }

    /// <summary>
    /// Verifies that an unterminated quote in a record after the header is reported on that record's line, counting the
    /// header row as line 1: line 2 for the first record, and line 3 for the second.
    /// </summary>
    [TestMethod]
    public void Read_WhenARecordAfterTheHeaderHasAnUnterminatedQuote_ShouldReportItsLine()
    {
        var inFirstRecord = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("a,b,c\n4,5,\"6");
        });

        var inSecondRecord = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("a,b,c\n1,2,3\n4,5,\"6");
        });

        Assert.AreEqual(2, inFirstRecord.LineNumber);
        Assert.AreEqual(3, inSecondRecord.LineNumber);
    }

    /// <summary>
    /// Reads every record into its decoded field values.
    /// </summary>
    /// <param name="source">The CSV source text.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The records, each as its field values in order.</returns>
    private static List<string[]> ReadRecords(string source, DelimitedReaderOptions options)
    {
        var reader = new Utf8DelimitedReader(Encoding.UTF8.GetBytes(source), options);
        var records = new List<string[]>();
        var fields = new List<string>();
        int depth = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DelimitedTokenType.StartArray:
                case DelimitedTokenType.StartObject:
                    depth++;
                    break;

                case DelimitedTokenType.String:
                    fields.Add(reader.GetString());
                    break;

                case DelimitedTokenType.EndArray:
                case DelimitedTokenType.EndObject:
                    if (depth == 2)
                    {
                        records.Add([.. fields]);
                        fields.Clear();
                    }

                    depth--;
                    break;

                default:
                    break;
            }
        }

        return records;
    }
}
