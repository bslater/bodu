// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.Reset.cs" company="Bodu Pty. Ltd.">
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
/// Verifies that <see cref="Utf8TomlWriter.Reset" /> prepares the writer for a second document.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8TomlWriter.Reset" /> re-arms the writer for a second document and clears the
    /// byte counts.
    /// </summary>
    [TestMethod]
    public void Reset_WhenDocumentComplete_ShouldAllowSecondDocument()
    {
        using var stream = new MemoryStream();
        var writer = new Utf8TomlWriter(stream);

        writer.WriteStartTable();
        writer.WriteInteger("a", 1);
        writer.WriteEndTable();
        writer.Flush();

        writer.Reset();
        Assert.AreEqual(0, writer.BytesCommitted);
        Assert.AreEqual(0, writer.BytesPending);

        writer.WriteStartTable();
        writer.WriteInteger("b", 2);
        writer.WriteEndTable();
        writer.Flush();

        Assert.AreEqual("a = 1\nb = 2\n", Encoding.UTF8.GetString(stream.ToArray()));
    }

}
