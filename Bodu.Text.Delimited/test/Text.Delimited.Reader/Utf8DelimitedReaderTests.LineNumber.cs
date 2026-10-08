// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.LineNumber.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DelimitedReader.LineNumber" />, verifying the line the reader
/// reports as it reads past line endings, inside quoted fields and out of them.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Verifies that at the end of the last record the reader reports line 3 when the third line has no line ending,
    /// and line 4 when it ends with CRLF, the line number being that of the read position.
    /// </summary>
    /// <param name="endsWithCrLf">Whether a CRLF follows the last record.</param>
    /// <param name="expectedLine">The line number expected at the end of the last record.</param>
    [TestMethod]
    [DataRow(false, 3)]
    [DataRow(true, 4)]
    public void LineNumber_WhenTheLastRecordHasNoLineEnding_ShouldCountItsLine(bool endsWithCrLf, int expectedLine)
    {
        byte[] bytes = Encoding.UTF8.GetBytes("A,B,C,D\r\na1,b1,c1,d1\r\na2,b2,c2,d2" + (endsWithCrLf ? "\r\n" : string.Empty));
        var reader = new Utf8DelimitedReader(bytes);
        int lineAtLastRecordEnd = 0;

        while (reader.Read())
        {
            if (reader.TokenType == DelimitedTokenType.EndObject)
                lineAtLastRecordEnd = reader.LineNumber;
        }

        Assert.AreEqual(expectedLine, lineAtLastRecordEnd);
    }

    /// <summary>
    /// Verifies that a line break inside a quoted field counts as a line: three records on four lines leave the reader
    /// on line 4, and an unterminated quote opened on the fourth line is reported on line 4.
    /// </summary>
    [TestMethod]
    public void LineNumber_WhenARecordHoldsAQuotedLineBreak_ShouldCountIt()
    {
        const string Source = "a,b,c\nd,\"e\n\",f\ng,h,i";
        var options = new DelimitedReaderOptions { NoHeader = true };
        var reader = new Utf8DelimitedReader(Encoding.UTF8.GetBytes(Source), options);

        while (reader.Read())
        {
        }

        Assert.AreEqual(3, ReadRecords(Source, options).Count);
        Assert.AreEqual(4, reader.LineNumber);

        var ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("a,b,c\nd,\"e\n\",f\ng,\"h", options);
        });

        Assert.AreEqual(4, ex.LineNumber);
    }
}
