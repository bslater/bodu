// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.BytesCommitted.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.BytesCommitted" /> reports committed byte counts.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that a buffer-writer destination commits bytes as the root table closes, with nothing pending and
    /// <see cref="Utf8TomlWriter.Flush" /> a no-op.
    /// </summary>
    [TestMethod]
    public void BytesCommitted_WhenBufferWriterDestination_ShouldCountAtRootClose()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);

        writer.WriteStartTable();
        writer.WriteInteger("a", 1);
        Assert.AreEqual(0, writer.BytesCommitted);

        writer.WriteEndTable();
        Assert.AreEqual(buffer.WrittenCount, writer.BytesCommitted);
        Assert.AreEqual(0, writer.BytesPending);

        writer.Flush();
        Assert.AreEqual(buffer.WrittenCount, writer.BytesCommitted);
    }

}
