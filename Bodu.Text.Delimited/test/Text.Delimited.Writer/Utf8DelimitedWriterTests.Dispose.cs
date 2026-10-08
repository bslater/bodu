// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedWriterTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Delimited.Writer;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DelimitedWriter.Dispose" />.
/// </summary>
public partial class Utf8DelimitedWriterTests
{
    /// <summary>
    /// Verifies that a stream-backed writer disposed twice neither throws nor writes its pending bytes a second time.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DelimitedWriter(destination);

        writer.WriteStartArray();
        WriteRecord(ref writer, ("name", "Ada"));
        writer.WriteEndArray();

        writer.Dispose();
        writer.Dispose();

        Assert.AreEqual("name\r\nAda\r\n", Encoding.UTF8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that disposing a stream-backed writer that holds a written record, without a call to
    /// <see cref="Utf8DelimitedWriter.Flush" />, leaves the record in the stream.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenBytesArePending_ShouldFlushThemToTheStream()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DelimitedWriter(destination);

        writer.WriteStartArray();
        WriteRecord(ref writer, ("name", "Ada"));
        writer.WriteEndArray();
        Assert.IsGreaterThan(0L, writer.BytesPending);

        writer.Dispose();

        Assert.AreEqual("name\r\nAda\r\n", Encoding.UTF8.GetString(destination.ToArray()));
    }
}
