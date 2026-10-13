// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8OutputSinkTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Serialization;

public sealed partial class Utf8OutputSinkTests
{
    /// <summary>
    /// Verifies that a new sink over either destination has committed and staged nothing.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNothingIsWritten_ShouldReportNoBytes()
    {
        var bufferSink = new Utf8OutputSink(new ArrayBufferWriter<byte>());
        using var destination = new MemoryStream();
        var streamSink = new Utf8OutputSink(destination);

        Assert.AreEqual(0L, bufferSink.BytesCommitted);
        Assert.AreEqual(0L, bufferSink.BytesPending);
        Assert.AreEqual(0L, streamSink.BytesCommitted);
        Assert.AreEqual(0L, streamSink.BytesPending);
    }
}
