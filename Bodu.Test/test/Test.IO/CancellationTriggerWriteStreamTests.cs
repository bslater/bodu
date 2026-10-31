// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CancellationTriggerWriteStreamTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// Verifies <see cref="CancellationTriggerWriteStream" />: that it cancels its source once the configured number of
/// bytes has been written, that later asynchronous writes and flushes with the canceled token throw
/// <see cref="OperationCanceledException" /> carrying it, and that a live token still writes.
/// </summary>
[TestClass]
public sealed class CancellationTriggerWriteStreamTests
{
    /// <summary>
    /// Verifies that writes below the threshold land without canceling the source.
    /// </summary>
    [TestMethod]
    public void Write_WhenBelowThreshold_ShouldWriteAndNotCancel()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new CancellationTriggerWriteStream(cts, cancelAfterBytes: 10);

        stream.Write(new byte[] { 1, 2, 3 }, 0, 3);
        stream.Write(new byte[] { 4 });

        Assert.AreEqual(4L, stream.BytesWritten);
        Assert.IsFalse(stream.HasTriggered);
        Assert.IsFalse(cts.IsCancellationRequested);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that the write carrying the total to the threshold lands in full and cancels the source as it returns.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task WriteAsync_WhenThresholdReached_ShouldWriteAndCancelSource()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new CancellationTriggerWriteStream(cts, cancelAfterBytes: 4);

        await stream.WriteAsync(new byte[] { 1, 2 }, cts.Token);
        Assert.IsFalse(cts.IsCancellationRequested);

        await stream.WriteAsync(new byte[] { 3, 4, 5 }, cts.Token);

        Assert.IsTrue(stream.HasTriggered);
        Assert.IsTrue(cts.IsCancellationRequested);
        Assert.AreEqual(5L, stream.BytesWritten);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that, once the source is canceled, an asynchronous write with its token throws
    /// <see cref="OperationCanceledException" /> carrying that token and writes nothing.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task WriteAsync_WhenSourceCanceled_ShouldThrowOperationCanceledExceptionWithToken()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new CancellationTriggerWriteStream(cts, cancelAfterBytes: 2);
        await stream.WriteAsync(new byte[] { 1, 2 }, cts.Token);

        var ex = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await stream.WriteAsync(new byte[] { 3 }, cts.Token);
        });

        var arrayEx = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await stream.WriteAsync(new byte[] { 3 }, 0, 1, cts.Token);
        });

        Assert.AreEqual(cts.Token, ex.CancellationToken);
        Assert.AreEqual(cts.Token, arrayEx.CancellationToken);
        Assert.AreEqual(2L, stream.BytesWritten);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that, once the source is canceled, an asynchronous flush with its token throws
    /// <see cref="OperationCanceledException" /> carrying that token.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task FlushAsync_WhenSourceCanceled_ShouldThrowOperationCanceledExceptionWithToken()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new CancellationTriggerWriteStream(cts, cancelAfterBytes: 1);
        stream.Write(new byte[] { 1 });

        var ex = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await stream.FlushAsync(cts.Token);
        });

        Assert.AreEqual(cts.Token, ex.CancellationToken);
    }

    /// <summary>
    /// Verifies that an asynchronous write with a live token succeeds after the source has been canceled, so a retry
    /// with a fresh token can be tested.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task WriteAsync_WhenTokenIsLiveAfterCancellation_ShouldWrite()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new CancellationTriggerWriteStream(cts, cancelAfterBytes: 1);
        await stream.WriteAsync(new byte[] { 1 }, cts.Token);

        await stream.WriteAsync(new byte[] { 2, 3 }, CancellationToken.None);
        await stream.FlushAsync(CancellationToken.None);

        Assert.AreEqual(3L, stream.BytesWritten);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that a stream seeded with content exposes it to reads from the start, counts none of it as written,
    /// and lets a consumer truncate and rewrite it in place.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSeededThenRewritten_ShouldReturnRewrittenBytes()
    {
        byte[] seed = { 10, 20, 30 };
        using var cts = new CancellationTokenSource();
        using var stream = new CancellationTriggerWriteStream(seed, cts, cancelAfterBytes: 100);

        Assert.AreEqual(0L, stream.Position);
        Assert.AreEqual(3L, stream.Length);
        Assert.AreEqual(0L, stream.BytesWritten);
        byte[] read = new byte[3];
        stream.ReadExactly(read);
        CollectionAssert.AreEqual(seed, read);

        stream.Position = 0;
        stream.SetLength(0);
        stream.Write(new byte[] { 1 });

        Assert.AreEqual(1L, stream.BytesWritten);
        CollectionAssert.AreEqual(new byte[] { 1 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> source is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            using var stream = new CancellationTriggerWriteStream(null!, cancelAfterBytes: 1);
        });

        Assert.AreEqual("cts", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a negative threshold is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCancelAfterBytesNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using var cts = new CancellationTokenSource();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            using var stream = new CancellationTriggerWriteStream(cts, cancelAfterBytes: -1);
        });

        Assert.AreEqual("cancelAfterBytes", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see langword="null" /> seed content is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialContentNull_ShouldThrowArgumentNullException()
    {
        using var cts = new CancellationTokenSource();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            using var stream = new CancellationTriggerWriteStream(null!, cts, cancelAfterBytes: 1);
        });

        Assert.AreEqual("initialContent", ex.ParamName);
    }
}
