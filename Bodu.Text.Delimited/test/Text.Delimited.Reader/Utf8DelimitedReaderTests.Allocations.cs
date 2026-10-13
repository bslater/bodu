// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Allocations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the allocation tests for <see cref="Utf8DelimitedReader" />, which pin the bytes it allocates to read a
/// document, counted with <see cref="GC.GetAllocatedBytesForCurrentThread" /> after a warm-up read, so that they do not
/// depend on timing.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>The number of records each allocation measurement reads.</summary>
    private const int AllocationRecordCount = 1000;

    /// <summary>
    /// Verifies that reading quoted fields of non-ASCII text allocates no more than reading quoted fields of ASCII text
    /// of the same length in characters, within 4 KiB, rather than a string for every non-ASCII character it decodes.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldsHoldNonAsciiText_ShouldAllocateNoMoreThanForAsciiText()
    {
        byte[] ascii = CreateQuotedRecords("abcdefg");
        byte[] nonAscii = CreateQuotedRecords("\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00C6");

        long asciiBytes = MeasureReadAllocations(ascii);
        long nonAsciiBytes = MeasureReadAllocations(nonAscii);

        Assert.IsLessThanOrEqualTo(asciiBytes + 4096, nonAsciiBytes, $"ASCII: {asciiBytes} B; non-ASCII: {nonAsciiBytes} B.");
    }

    /// <summary>
    /// Verifies that reading quoted fields allocates little more than the strings of the fields read, 40 bytes for the
    /// quoted seven-character text and 24 for the one-character number of each record, within 4 KiB for the document,
    /// rather than a builder for every quoted field.
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedFieldsHoldAsciiText_ShouldAllocateLittleMoreThanTheFieldStrings()
    {
        byte[] ascii = CreateQuotedRecords("abcdefg");

        long allocated = MeasureReadAllocations(ascii);

        Assert.IsLessThanOrEqualTo((AllocationRecordCount * (40 + 24)) + 4096L, allocated, $"Allocated {allocated} B.");
    }

    /// <summary>
    /// Creates a headerless document of <see cref="AllocationRecordCount" /> records, each a quoted field holding the
    /// supplied text and an unquoted number.
    /// </summary>
    /// <param name="text">The text of each quoted field.</param>
    /// <returns>The document's UTF-8 bytes.</returns>
    private static byte[] CreateQuotedRecords(string text)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < AllocationRecordCount; i++)
            builder.Append('"').Append(text).Append("\",1\r\n");

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Reads a headerless document to its end, once to warm up and once measured, and returns the bytes the measured
    /// read allocated on the current thread.
    /// </summary>
    /// <param name="document">The document's UTF-8 bytes.</param>
    /// <returns>The bytes allocated by the measured read.</returns>
    private static long MeasureReadAllocations(byte[] document)
    {
        var options = new DelimitedReaderOptions { NoHeader = true };
        ReadToEnd(document, options);

        long before = GC.GetAllocatedBytesForCurrentThread();
        ReadToEnd(document, options);

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    /// <summary>
    /// Reads every token of a document, decoding each string token.
    /// </summary>
    /// <param name="document">The document's UTF-8 bytes.</param>
    /// <param name="options">The reader options.</param>
    private static void ReadToEnd(byte[] document, DelimitedReaderOptions options)
    {
        var reader = new Utf8DelimitedReader(document, options);
        while (reader.Read())
        {
            if (reader.TokenType == DelimitedTokenType.String)
                _ = reader.GetString();
        }
    }
}
