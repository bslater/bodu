// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedWriterTests.WriteString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

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
}
