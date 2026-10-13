// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.Allocations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Contains the allocation tests for <see cref="Utf8DotEnvReader" />, which pin the bytes it allocates to read a
/// document, counted with <see cref="GC.GetAllocatedBytesForCurrentThread" /> after a warm-up read, so that they do not
/// depend on timing.
/// </summary>
public partial class Utf8DotEnvReaderTests
{
    /// <summary>The number of entries each allocation measurement reads.</summary>
    private const int AllocationEntryCount = 1000;

    /// <summary>
    /// Verifies that reading double-quoted values allocates no more than reading the same text unquoted, within 4 KiB,
    /// rather than a builder for every value.
    /// </summary>
    [TestMethod]
    public void Read_WhenValuesAreDoubleQuoted_ShouldAllocateNoMoreThanForUnquotedValues()
    {
        const string Text = "café au lait ☕ number";
        byte[] unquoted = CreateEntries(Text);
        byte[] quoted = CreateEntries($"\"{Text}\"");

        long unquotedBytes = MeasureReadAllocations(unquoted);
        long quotedBytes = MeasureReadAllocations(quoted);

        Assert.IsLessThanOrEqualTo(unquotedBytes + 4096, quotedBytes, $".NET {Environment.Version}; unquoted: {unquotedBytes} B; double-quoted: {quotedBytes} B.");
    }

    /// <summary>
    /// Verifies that reading double-quoted values that hold escape sequences allocates no more than reading unquoted
    /// values that read as text of the same length, within 4 KiB, rather than a builder for every value.
    /// </summary>
    [TestMethod]
    public void Read_WhenDoubleQuotedValuesHoldEscapes_ShouldAllocateNoMoreThanForUnquotedValues()
    {
        byte[] unquoted = CreateEntries("café x lait y ☕");
        byte[] quoted = CreateEntries("\"café \\t lait \\\" ☕\"");

        long unquotedBytes = MeasureReadAllocations(unquoted);
        long quotedBytes = MeasureReadAllocations(quoted);

        Assert.IsLessThanOrEqualTo(unquotedBytes + 4096, quotedBytes, $".NET {Environment.Version}; unquoted: {unquotedBytes} B; double-quoted: {quotedBytes} B.");
    }

    /// <summary>
    /// Creates a document of <see cref="AllocationEntryCount" /> entries, each with the supplied value as written.
    /// </summary>
    /// <param name="value">The value of each entry, as written after the <c>=</c>.</param>
    /// <returns>The document's UTF-8 bytes.</returns>
    private static byte[] CreateEntries(string value)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < AllocationEntryCount; i++)
            builder.Append("KEY=").Append(value).Append('\n');

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Reads a document to its end, once to warm up and once measured, and returns the bytes the measured read
    /// allocated on the current thread.
    /// </summary>
    /// <param name="document">The document's UTF-8 bytes.</param>
    /// <returns>The bytes allocated by the measured read.</returns>
    private static long MeasureReadAllocations(byte[] document)
    {
        ReadValues(document);

        long before = GC.GetAllocatedBytesForCurrentThread();
        ReadValues(document);

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    /// <summary>
    /// Reads every token of a document, decoding each value.
    /// </summary>
    /// <param name="document">The document's UTF-8 bytes.</param>
    private static void ReadValues(byte[] document)
    {
        var reader = new Utf8DotEnvReader(document);
        while (reader.Read())
        {
            if (reader.TokenType == DotEnvTokenType.String)
                _ = reader.GetString();
        }
    }
}
