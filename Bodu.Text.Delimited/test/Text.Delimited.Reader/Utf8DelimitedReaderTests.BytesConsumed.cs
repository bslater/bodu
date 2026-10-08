// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.BytesConsumed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DelimitedReader.BytesConsumed" />, verifying the count after
/// records that hold quoted, empty quoted and trimmed fields.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Verifies that trimming does not shorten the byte count: after reading <c>1, 2</c> and its CRLF with
    /// <see cref="DelimitedReaderOptions.TrimFields" />, the reader has consumed all six bytes of the line, while the
    /// fields themselves are trimmed.
    /// </summary>
    [TestMethod]
    public void BytesConsumed_WhenFieldsAreTrimmed_ShouldCountEveryByteRead()
    {
        const string Source = "1, 2\r\n";
        var options = new DelimitedReaderOptions { NoHeader = true, TrimFields = true };

        List<int> consumed = BytesConsumedAtRecordEnds(Source, options);

        CollectionAssert.AreEqual(new List<int> { 6 }, consumed);
        CollectionAssert.AreEqual(new[] { "1", "2" }, ReadRecords(Source, options)[0]);
    }

    /// <summary>
    /// Verifies that the byte count after each record counts every quote of its quoted fields once, empty quoted fields
    /// included: 8 bytes after <c>1,"",2</c> and its CRLF, and 19 after <c>"3",4,"5"</c> and its CRLF.
    /// </summary>
    [TestMethod]
    public void BytesConsumed_WhenRecordsHoldEmptyQuotedFields_ShouldCountEachByteOnce()
    {
        List<int> consumed = BytesConsumedAtRecordEnds("1,\"\",2\r\n\"3\",4,\"5\"\r\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(new List<int> { 8, 19 }, consumed);
    }

    /// <summary>
    /// Verifies that the byte count after each record counts a closing quote once: 7 bytes after <c>1,"2"</c> and its
    /// CRLF, and 14 after <c>"3",4</c> and its CRLF.
    /// </summary>
    [TestMethod]
    public void BytesConsumed_WhenRecordsHoldQuotedFields_ShouldCountEachByteOnce()
    {
        List<int> consumed = BytesConsumedAtRecordEnds("1,\"2\"\r\n\"3\",4\r\n", new DelimitedReaderOptions { NoHeader = true });

        CollectionAssert.AreEqual(new List<int> { 7, 14 }, consumed);
    }

    /// <summary>
    /// Reads every token and records <see cref="Utf8DelimitedReader.BytesConsumed" /> at the end of each record.
    /// </summary>
    /// <param name="source">The CSV source text.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The byte count at the end token of each record, in order.</returns>
    private static List<int> BytesConsumedAtRecordEnds(string source, DelimitedReaderOptions options)
    {
        var reader = new Utf8DelimitedReader(Encoding.UTF8.GetBytes(source), options);
        var consumed = new List<int>();
        int depth = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DelimitedTokenType.StartArray:
                case DelimitedTokenType.StartObject:
                    depth++;
                    break;

                case DelimitedTokenType.EndArray:
                case DelimitedTokenType.EndObject:
                    if (depth == 2)
                        consumed.Add(reader.BytesConsumed);

                    depth--;
                    break;

                default:
                    break;
            }
        }

        return consumed;
    }
}
