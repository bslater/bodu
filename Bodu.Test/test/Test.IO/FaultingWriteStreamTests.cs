// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FaultingWriteStreamTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// Verifies <see cref="FaultingWriteStream" />: that writes under the limit land, that the write crossing it throws
/// before writing anything, that flushes fault when configured, that disarming lets a retry through, and that the
/// destination's bytes and counters report what happened.
/// </summary>
[TestClass]
public sealed class FaultingWriteStreamTests
{
    /// <summary>
    /// Verifies that writes whose bytes fit under the limit succeed, land in the destination and are counted.
    /// </summary>
    [TestMethod]
    public void Write_WhenBelowLimit_ShouldWriteAndCount()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 10);

        stream.Write(new byte[] { 1, 2, 3 }, 0, 3);
        stream.Write(new byte[] { 4, 5 });

        Assert.AreEqual(5L, stream.BytesWritten);
        Assert.AreEqual(2, stream.WriteCount);
        Assert.AreEqual(5L, stream.Length);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that the write that would carry the total past the limit throws <see cref="IOException" /> before any
    /// of its bytes land, and that a later write that would cross it throws the same way.
    /// </summary>
    [TestMethod]
    public void Write_WhenCrossingLimit_ShouldThrowIOExceptionAndWriteNothing()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 4);
        stream.Write(new byte[] { 1, 2, 3 }, 0, 3);

        Assert.ThrowsExactly<IOException>(() =>
        {
            stream.Write(new byte[] { 4, 5 }, 0, 2);
        });

        Assert.ThrowsExactly<IOException>(() =>
        {
            stream.Write(new byte[] { 9, 9 });
        });

        Assert.AreEqual(3L, stream.BytesWritten);
        Assert.AreEqual(3, stream.WriteCount);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that a write landing exactly on the limit succeeds, and only the next write faults.
    /// </summary>
    [TestMethod]
    public void Write_WhenReachingLimitExactly_ShouldWriteAndFaultOnNextWrite()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 4);

        stream.Write(new byte[] { 1, 2, 3, 4 });

        Assert.AreEqual(4L, stream.BytesWritten);
        Assert.ThrowsExactly<IOException>(() =>
        {
            stream.Write(new byte[] { 5 });
        });
    }

    /// <summary>
    /// Verifies that the asynchronous write that would carry the total past the limit throws <see cref="IOException" />
    /// unwrapped and writes nothing.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task WriteAsync_WhenCrossingLimit_ShouldThrowIOExceptionAndWriteNothing()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 4);
        await stream.WriteAsync(new byte[] { 1, 2, 3 });

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
        {
            await stream.WriteAsync(new byte[] { 4, 5 });
        });

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
        {
            await stream.WriteAsync(new byte[] { 4, 5 }, 0, 2, CancellationToken.None);
        });

        Assert.AreEqual(3L, stream.BytesWritten);
        Assert.AreEqual(3, stream.WriteCount);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="FaultingWriteStream.Flush" /> and <see cref="FaultingWriteStream.FlushAsync" /> throw
    /// <see cref="IOException" /> when the stream is configured to fault on flush, and are counted.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task Flush_WhenConfiguredToThrow_ShouldThrowIOException()
    {
        using var stream = new FaultingWriteStream(long.MaxValue, throwOnFlush: true);
        stream.Write(new byte[] { 1, 2, 3 });

        Assert.ThrowsExactly<IOException>(() =>
        {
            stream.Flush();
        });

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
        {
            await stream.FlushAsync(CancellationToken.None);
        });

        Assert.AreEqual(2, stream.FlushCount);
        Assert.AreEqual(3L, stream.BytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="FaultingWriteStream.Flush" /> succeeds when the stream is not configured to fault on
    /// flush, whatever the write limit.
    /// </summary>
    [TestMethod]
    public void Flush_WhenNotConfiguredToThrow_ShouldSucceed()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 0);

        stream.Flush();

        Assert.AreEqual(1, stream.FlushCount);
        Assert.IsTrue(stream.IsArmed);
    }

    /// <summary>
    /// Verifies that <see cref="FaultingWriteStream.Disarm" /> lets the next write and flush succeed after a fault,
    /// keeping the bytes already written and counting on.
    /// </summary>
    [TestMethod]
    public void Disarm_WhenCalledAfterFault_ShouldLetNextWriteAndFlushSucceed()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 2, throwOnFlush: true);
        stream.Write(new byte[] { 1, 2 });
        Assert.ThrowsExactly<IOException>(() =>
        {
            stream.Write(new byte[] { 3 });
        });

        Assert.ThrowsExactly<IOException>(() =>
        {
            stream.Flush();
        });

        stream.Disarm();
        stream.Write(new byte[] { 3, 4 });
        stream.Flush();

        Assert.IsFalse(stream.IsArmed);
        Assert.AreEqual(4L, stream.BytesWritten);
        Assert.AreEqual(3, stream.WriteCount);
        Assert.AreEqual(2, stream.FlushCount);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that a stream seeded with content exposes it to reads from the start, counts none of it as written,
    /// and lets a consumer truncate and rewrite it in place.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSeededThenRewritten_ShouldReturnRewrittenBytes()
    {
        byte[] seed = { 10, 20, 30, 40, 50 };
        using var stream = new FaultingWriteStream(seed, throwAfterBytes: 100);

        Assert.AreEqual(0L, stream.Position);
        Assert.AreEqual(5L, stream.Length);
        Assert.AreEqual(0L, stream.BytesWritten);
        byte[] read = new byte[5];
        stream.ReadExactly(read);
        CollectionAssert.AreEqual(seed, read);

        stream.Position = 0;
        stream.SetLength(0);
        stream.Write(new byte[] { 1, 2 });

        Assert.AreEqual(2L, stream.BytesWritten);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that a non-seekable stream reports <see cref="FaultingWriteStream.CanSeek" /> as <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void CanSeek_WhenNotSeekable_ShouldBeFalse()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 100, seekable: false);

        Assert.IsFalse(stream.CanSeek);
    }

    /// <summary>
    /// Verifies that a non-seekable stream reports <see cref="FaultingWriteStream.CanRead" /> as <see langword="false" />,
    /// since it is a write-only sink.
    /// </summary>
    [TestMethod]
    public void CanRead_WhenNotSeekable_ShouldBeFalse()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 100, seekable: false);

        Assert.IsFalse(stream.CanRead);
    }

    /// <summary>
    /// Verifies that a non-seekable stream still reports <see cref="FaultingWriteStream.CanWrite" /> as
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void CanWrite_WhenNotSeekable_ShouldBeTrue()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 100, seekable: false);

        Assert.IsTrue(stream.CanWrite);
    }

    /// <summary>
    /// Verifies that <see cref="FaultingWriteStream.Seek" /> on a non-seekable stream throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Seek_WhenNotSeekable_ShouldThrowNotSupportedException()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 100, seekable: false);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = stream.Seek(0, SeekOrigin.Begin);
        });
    }

    /// <summary>
    /// Verifies that <see cref="FaultingWriteStream.Length" /> on a non-seekable stream throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Length_WhenNotSeekable_ShouldThrowNotSupportedException()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 100, seekable: false);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = stream.Length;
        });
    }

    /// <summary>
    /// Verifies that reading and setting <see cref="FaultingWriteStream.Position" /> on a non-seekable stream throw
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Position_WhenNotSeekable_ShouldThrowNotSupportedException()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 100, seekable: false);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = stream.Position;
        });

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            stream.Position = 0;
        });
    }

    /// <summary>
    /// Verifies that <see cref="FaultingWriteStream.SetLength" /> on a non-seekable stream throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenNotSeekable_ShouldThrowNotSupportedException()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 100, seekable: false);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            stream.SetLength(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="FaultingWriteStream.Read(byte[], int, int)" /> on a non-seekable stream throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenNotSeekable_ShouldThrowNotSupportedException()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 100, seekable: false);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = stream.Read(new byte[1], 0, 1);
        });
    }

    /// <summary>
    /// Verifies that writes to a non-seekable stream append in order and are reported through
    /// <see cref="FaultingWriteStream.ToArray" />.
    /// </summary>
    [TestMethod]
    public void Write_WhenNotSeekable_ShouldAppendAndReportThroughToArray()
    {
        using var stream = new FaultingWriteStream(throwAfterBytes: 100, seekable: false);

        stream.Write(new byte[] { 1, 2 });
        stream.Write(new byte[] { 3 });

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, stream.ToArray());
    }

    /// <summary>
    /// Verifies that a negative limit is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenThrowAfterBytesNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            using var stream = new FaultingWriteStream(throwAfterBytes: -1);
        });

        Assert.AreEqual("throwAfterBytes", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see langword="null" /> seed content is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialContentNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            using var stream = new FaultingWriteStream(null!, throwAfterBytes: 1);
        });

        Assert.AreEqual("initialContent", ex.ParamName);
    }
}
