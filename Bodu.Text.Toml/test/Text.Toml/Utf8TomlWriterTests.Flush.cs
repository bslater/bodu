// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.Flush.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.Flush" /> delivers buffered bytes to the destination.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that a stream-backed writer buffers the rendered document and delivers it on
    /// <see cref="Utf8TomlWriter.Flush" />, with <see cref="Utf8TomlWriter.BytesPending" /> and
    /// <see cref="Utf8TomlWriter.BytesCommitted" /> tracking the transition.
    /// </summary>
    [TestMethod]
    public void Flush_WhenStreamDestination_ShouldDeliverBufferedBytes()
    {
        using var stream = new MemoryStream();
        var writer = new Utf8TomlWriter(stream);

        writer.WriteStartTable();
        writer.WriteInteger("a", 1);
        Assert.AreEqual(0, writer.BytesPending);

        writer.WriteEndTable();
        Assert.AreEqual(0, stream.Length);
        Assert.AreEqual(6, writer.BytesPending);
        Assert.AreEqual(0, writer.BytesCommitted);

        writer.Flush();
        Assert.AreEqual("a = 1\n", Encoding.UTF8.GetString(stream.ToArray()));
        Assert.AreEqual(0, writer.BytesPending);
        Assert.AreEqual(6, writer.BytesCommitted);
    }

}
