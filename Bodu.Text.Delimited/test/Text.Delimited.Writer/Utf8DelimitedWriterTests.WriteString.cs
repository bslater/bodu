// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedWriterTests.WriteString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Writer;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DelimitedWriter.WriteString(string)" />.
/// </summary>
public partial class Utf8DelimitedWriterTests
{
    /// <summary>
    /// Verifies that a record holding a field of 1,000 lowercase letters, written to a stream, comes out whole, however
    /// the writer stages its bytes before they reach the stream.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenAFieldIsLongerThanTheScratchBuffer_ShouldWriteItWhole()
    {
        var random = new Random(1000);
        string letters = string.Create(1000, random, static (span, source) =>
        {
            for (int i = 0; i < span.Length; i++)
                span[i] = (char)('a' + source.Next(26));
        });

        using var destination = new MemoryStream();
        var writer = new Utf8DelimitedWriter(destination);

        writer.WriteStartArray();
        writer.WriteStartArray();
        writer.WriteString("one");
        writer.WriteString(letters);
        writer.WriteEndArray();
        writer.WriteEndArray();
        writer.Flush();

        Assert.AreEqual("one," + letters + "\r\n", Encoding.UTF8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that a record whose only field is empty is written as <c>""</c> rather than as an empty line, which the
    /// reader skips, so the records <c>[a]</c>, <c>[""]</c> and <c>[b]</c> read back as three records.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenARecordsOnlyFieldIsEmpty_ShouldQuoteItSoItReadsBack()
    {
        string[][] records = [["a"], [""], ["b"]];

        string text = WritePositionalRecords(DelimitedWriterOptions.Default, records);

        Assert.AreEqual("a\r\n\"\"\r\nb\r\n", text);
        AssertReadBack(records, text, new DelimitedReaderOptions { NoHeader = true });
    }

    /// <summary>
    /// Verifies that an empty field in a record of several fields is still written bare: the records <c>[a, ""]</c> and
    /// <c>["", d]</c> are written <c>a,</c> and <c>,d</c>.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenAnEmptyFieldHasOtherFieldsBesideIt_ShouldWriteItBare()
    {
        string text = WritePositionalRecords(DelimitedWriterOptions.Default, [["a", ""], ["", "d"]]);

        Assert.AreEqual("a,\r\n,d\r\n", text);
    }

    /// <summary>
    /// Verifies that a record's first field beginning with the comment character, <c>#</c> by default, is quoted, so a
    /// reader that allows comments reads the record rather than skipping it as a comment, while a later field beginning
    /// with <c>#</c> stays bare.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenTheFirstFieldBeginsWithTheCommentChar_ShouldQuoteItSoItReadsBack()
    {
        string[][] records = [["#no comment", "#x"], ["a", "#b"]];

        string text = WritePositionalRecords(DelimitedWriterOptions.Default, records);

        Assert.AreEqual("\"#no comment\",#x\r\na,#b\r\n", text);
        AssertReadBack(records, text, new DelimitedReaderOptions { NoHeader = true, AllowComments = true });
    }

    /// <summary>
    /// Verifies that a field beginning or ending with a space or a tab is quoted, so a reader with
    /// <see cref="DelimitedReaderOptions.TrimFields" /> reads it back unchanged, while a field with white space only
    /// inside it stays bare.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenAFieldBeginsOrEndsWithWhiteSpace_ShouldQuoteItSoItReadsBack()
    {
        string[][] records = [[" a ", "b\t", "\tc", "d e", "   "]];

        string text = WritePositionalRecords(DelimitedWriterOptions.Default, records);

        Assert.AreEqual("\" a \",\"b\t\",\"\tc\",d e,\"   \"\r\n", text);
        AssertReadBack(records, text, new DelimitedReaderOptions { NoHeader = true, TrimFields = true });
    }

    /// <summary>
    /// Verifies that with <see cref="DelimitedWriterOptions.CommentChar" /> set to <c>;</c> the writer quotes a first
    /// field beginning with <c>;</c> instead of one beginning with <c>#</c>, so a reader with the same comment
    /// character reads both records back.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenTheCommentCharIsSet_ShouldQuoteAFirstFieldBeginningWithIt()
    {
        string[][] records = [[";comment-like", "b"], ["#x", "c"]];

        string text = WritePositionalRecords(new DelimitedWriterOptions { CommentChar = ';' }, records);

        Assert.AreEqual("\";comment-like\",b\r\n#x,c\r\n", text);
        AssertReadBack(records, text, new DelimitedReaderOptions { NoHeader = true, AllowComments = true, CommentChar = ';' });
    }

    /// <summary>
    /// Writes positional records and returns the text written.
    /// </summary>
    /// <param name="options">The writer options.</param>
    /// <param name="records">The records, each as its field values.</param>
    /// <returns>The delimited text.</returns>
    private static string WritePositionalRecords(DelimitedWriterOptions options, string[][] records)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer, options);

        writer.WriteStartArray();
        foreach (string[] record in records)
        {
            writer.WriteStartArray();
            foreach (string field in record)
                writer.WriteString(field);

            writer.WriteEndArray();
        }

        writer.WriteEndArray();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Asserts that written text reads back, positionally, as exactly the records written.
    /// </summary>
    /// <param name="expected">The records written, each as its field values.</param>
    /// <param name="text">The text written.</param>
    /// <param name="options">The reader options, which must read positionally.</param>
    private static void AssertReadBack(string[][] expected, string text, DelimitedReaderOptions options)
    {
        var reader = new Utf8DelimitedReader(Encoding.UTF8.GetBytes(text), options);
        var records = new List<string[]>();
        var fields = new List<string>();
        int depth = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DelimitedTokenType.StartArray:
                    depth++;
                    break;

                case DelimitedTokenType.String:
                    fields.Add(reader.GetString());
                    break;

                case DelimitedTokenType.EndArray:
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

        Assert.AreEqual(expected.Length, records.Count, "The number of records read back differs.");
        for (int i = 0; i < expected.Length; i++)
            CollectionAssert.AreEqual(expected[i], records[i], $"Record {i} read back differently.");
    }
}
