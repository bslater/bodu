// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStreamTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public sealed partial class HashingStreamTests
{
    /// <summary>
    /// Verifies that disposal with <c>leaveOpen: true</c> keeps the inner stream usable.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenLeaveOpenIsTrue_ShouldKeepInnerStreamUsable()
    {
        using MemoryStream inner = new();

        using (HashingStream stream = new(inner, new Fnv1a32(), leaveOpen: true))
            stream.WriteByte(0x01);

        inner.WriteByte(0x02);

        Assert.AreEqual(2, inner.Length);
    }

    /// <summary>
    /// Verifies that disposal with the default <c>leaveOpen</c> also disposes the inner stream.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenLeaveOpenIsDefault_ShouldDisposeInnerStream()
    {
        MemoryStream inner = new();

        using (HashingStream stream = new(inner, new Fnv1a32()))
            stream.WriteByte(0x01);

        Assert.IsFalse(inner.CanRead);
    }

    /// <summary>
    /// Verifies that disposing the stream twice does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var stream = new HashingStream(new MemoryStream(), new Fnv1a32());

        stream.Dispose();
        stream.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="HashingStream.DisposeAsync" /> with <c>leaveOpen: true</c> keeps the inner stream
    /// usable.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DisposeAsync_WhenLeaveOpenIsTrue_ShouldKeepInnerStreamUsable()
    {
        using MemoryStream inner = new();

        await using (HashingStream stream = new(inner, new Fnv1a32(), leaveOpen: true))
            stream.WriteByte(0x01);

        inner.WriteByte(0x02);

        Assert.AreEqual(2, inner.Length);
    }

    /// <summary>
    /// Verifies that the transfer, digest, and capability members throw <see cref="ObjectDisposedException" /> or
    /// report unavailability after disposal.
    /// </summary>
    [TestMethod]
    public void Members_WhenDisposed_ShouldThrowObjectDisposedExceptionOrReportUnavailable()
    {
        var stream = new HashingStream(new MemoryStream(), new Fnv1a32());
        stream.Dispose();

        Assert.IsFalse(stream.CanRead);
        Assert.IsFalse(stream.CanWrite);

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = stream.Read(new byte[1], 0, 1);
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            stream.Write(new byte[1], 0, 1);
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = stream.GetCurrentHash();
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = stream.GetHashAndReset();
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            stream.Flush();
        });
    }
}
