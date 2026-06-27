// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.CurrentDepth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter.CurrentDepth" /> reports the current container nesting depth.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8BencodeWriter.CurrentDepth" /> tracks container opens and closes, returning to
    /// zero when the document completes — the completeness assertion available to manual writer callers.
    /// </summary>
    [TestMethod]
    public void CurrentDepth_WhenContainersOpenAndClose_ShouldTrackNesting()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);

        Assert.AreEqual(0, writer.CurrentDepth);

        writer.WriteStartDictionary();
        Assert.AreEqual(1, writer.CurrentDepth);

        writer.WritePropertyName("a");
        writer.WriteStartList();
        Assert.AreEqual(2, writer.CurrentDepth);

        writer.WriteInteger(1);
        writer.WriteEndList();
        Assert.AreEqual(1, writer.CurrentDepth);

        writer.WriteEndDictionary();
        Assert.AreEqual(0, writer.CurrentDepth);
    }

}
