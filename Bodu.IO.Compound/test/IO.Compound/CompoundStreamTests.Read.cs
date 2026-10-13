// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies reading from a writable cursor, which serves its bytes from the in-memory staging buffer rather than from
/// the container's sector chain.
/// </summary>
/// <remarks>
/// Whether a writable cursor may be read is decided by the <see cref="FileAccess" /> it was opened with, not by the
/// fact that it is writable - so a write-only cursor refuses reads even though the bytes are sitting in memory.
/// </remarks>
public partial class CompoundStreamTests
{
    /// <summary>
    /// Verifies that a read-write cursor reads back what was just written to it, from the position the caller seeks
    /// to.
    /// </summary>
    [TestMethod]
    public void Read_WhenCursorIsWritableWithReadAccess_ShouldReturnStagedBytes()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        using CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Create, FileAccess.ReadWrite);

        cursor.Write(new byte[] { 1, 2, 3, 4 });
        cursor.Position = 1;

        byte[] buffer = new byte[3];
        Assert.AreEqual(3, cursor.Read(buffer));
        CollectionAssert.AreEqual(new byte[] { 2, 3, 4 }, buffer);
    }

    /// <summary>
    /// Verifies that a write-only cursor refuses reads with <see cref="NotSupportedException" />, matching the
    /// <see cref="Stream" /> contract for a stream whose <see cref="Stream.CanRead" /> is
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenCursorIsWriteOnly_ShouldThrowNotSupportedException()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        using CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Create, FileAccess.Write);

        cursor.Write(new byte[] { 1, 2, 3 });
        cursor.Position = 0;

        Assert.IsFalse(cursor.CanRead);
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = cursor.Read(new byte[3]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStream.AsMemory" /> on a writable cursor exposes exactly the staged bytes,
    /// not the whole capacity of the buffer behind them.
    /// </summary>
    [TestMethod]
    public void AsMemory_WhenCursorIsWritable_ShouldExposeStagedLengthOnly()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        using CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Create, FileAccess.ReadWrite);

        cursor.Write(new byte[] { 1, 2, 3 });

        ReadOnlyMemory<byte> staged = cursor.AsMemory();

        Assert.AreEqual(3, staged.Length);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, staged.ToArray());
    }

    /// <summary>
    /// Verifies that a pre-canceled token cancels the array-based write before any byte reaches the buffer.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task WriteAsyncArray_WhenTokenIsAlreadyCanceled_ShouldCancelWithoutWriting()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        using CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Create, FileAccess.ReadWrite);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await cursor.WriteAsync(new byte[] { 1, 2, 3 }, 0, 3, cts.Token);
        });

        Assert.AreEqual(0L, cursor.Length);
    }

    /// <summary>
    /// Verifies that a pre-canceled token cancels the <see cref="ReadOnlyMemory{T}" /> write before any byte reaches
    /// the buffer.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task WriteAsyncMemory_WhenTokenIsAlreadyCanceled_ShouldCancelWithoutWriting()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        using CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Create, FileAccess.ReadWrite);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await cursor.WriteAsync(new byte[] { 1, 2, 3 }.AsMemory(), cts.Token);
        });

        Assert.AreEqual(0L, cursor.Length);
    }

    /// <summary>
    /// Verifies that a pre-canceled token cancels the flush rather than committing the staged bytes.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task FlushAsync_WhenTokenIsAlreadyCanceled_ShouldCancel()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        using CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Create, FileAccess.ReadWrite);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await cursor.FlushAsync(cts.Token);
        });
    }

    /// <summary>
    /// Verifies that a write on a read-only cursor faults the returned task rather than throwing synchronously, so an
    /// awaiting caller observes the failure through the task.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task WriteAsyncMemory_WhenCursorIsReadOnly_ShouldFaultTheTask()
    {
        byte[] bytes = BuildContainer(out _);
        using var file = CompoundFile.Open(new MemoryStream(bytes));
        using CompoundStream cursor = file.RootStorage.OpenStream("Data");

        _ = await Assert.ThrowsExactlyAsync<NotSupportedException>(async () =>
        {
            await cursor.WriteAsync(new byte[] { 1 }.AsMemory());
        });
    }

    /// <summary>
    /// Verifies that a read asking for more bytes than remain returns only the remaining bytes, and that the next read
    /// returns none.
    /// </summary>
    [TestMethod]
    public void Read_WhenCountExceedsRemainingBytes_ShouldReturnOnlyTheRemainingBytes()
    {
        byte[] payload = CompoundContainerBytes.Pattern(100);
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("Data", payload);
        using var file = CompoundFile.Open(new MemoryStream(builder.ToArray()));
        using CompoundStream stream = file.RootStorage.OpenStream("Data");
        stream.Position = 90;

        byte[] buffer = new byte[64];
        int first = stream.Read(buffer, 0, buffer.Length);
        int second = stream.Read(buffer, 0, buffer.Length);

        Assert.AreEqual(10, first);
        CollectionAssert.AreEqual(payload[90..], buffer[..10]);
        Assert.AreEqual(0, second);
    }

    /// <summary>
    /// Verifies that cursors over the streams of one file each read from their own position, however their reads and
    /// seeks interleave.
    /// </summary>
    /// <param name="strategy">The read strategy the file is opened with.</param>
    [TestMethod]
    [DataRow(CompoundReadStrategy.Buffered)]
    [DataRow(CompoundReadStrategy.Streaming)]
    public void Read_WhenTwoCursorsInterleave_ShouldReadEachFromItsOwnPosition(CompoundReadStrategy strategy)
    {
        byte[] a = CompoundContainerBytes.Pattern(5000);
        byte[] b = CompoundContainerBytes.Pattern(5000, 5000);
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", a);
        _ = builder.AddStream("B", b);
        var options = new CompoundFileOptions { ReadStrategy = strategy };
        using var file = CompoundFile.Open(new MemoryStream(builder.ToArray()), options);
        using CompoundStream firstA = file.RootStorage.OpenStream("A");
        using CompoundStream onlyB = file.RootStorage.OpenStream("B");
        using CompoundStream secondA = file.RootStorage.OpenStream("A");

        for (int round = 0; round < 2; round++)
        {
            CollectionAssert.AreEqual(a[(round * 100)..((round + 1) * 100)], ReadExactly(firstA, 100));
            CollectionAssert.AreEqual(b[(round * 100)..((round + 1) * 100)], ReadExactly(onlyB, 100));
            CollectionAssert.AreEqual(a[(round * 100)..((round + 1) * 100)], ReadExactly(secondA, 100));
        }

        firstA.Seek(4000, SeekOrigin.Begin);
        CollectionAssert.AreEqual(b[200..300], ReadExactly(onlyB, 100));
        CollectionAssert.AreEqual(a[4000..4100], ReadExactly(firstA, 100));
        CollectionAssert.AreEqual(a[200..300], ReadExactly(secondA, 100));
    }

    /// <summary>
    /// Verifies that a cursor over a version-4 stream, wrapped in a <see cref="BufferedStream" />, copies its whole
    /// payload.
    /// </summary>
    /// <param name="strategy">The read strategy the file is opened with.</param>
    [TestMethod]
    [DataRow(CompoundReadStrategy.Buffered)]
    [DataRow(CompoundReadStrategy.Streaming)]
    public void Read_WhenWrappedInBufferedStream_ShouldCopyWholePayload(CompoundReadStrategy strategy)
    {
        byte[] payload = CompoundContainerBytes.Pattern(86016);
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("Data", payload);
        byte[] container = builder.ToArray(new CompoundBuildOptions { Version = CompoundFileVersion.V4 });
        var options = new CompoundFileOptions { ReadStrategy = strategy };
        using var file = CompoundFile.Open(new MemoryStream(container), options);
        using CompoundStream stream = file.RootStorage.OpenStream("Data");
        using var buffered = new BufferedStream(stream);
        using var copy = new MemoryStream();

        buffered.CopyTo(copy);

        CollectionAssert.AreEqual(payload, copy.ToArray());
    }

    /// <summary>
    /// Verifies that a writable cursor still reads its own bytes after the file that owns it has been disposed.
    /// </summary>
    [TestMethod]
    public void Read_WhenOwningFileOfAWritableCursorIsDisposed_ShouldReturnTheCursorsBytes()
    {
        using CompoundStream cursor = OpenCursorOverDisposedFile();

        Assert.IsTrue(cursor.CanRead);
        Assert.AreEqual(3, cursor.Read(new byte[4], 0, 4));
    }

    /// <summary>
    /// Reads exactly the specified number of bytes from a stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>The bytes read.</returns>
    private static byte[] ReadExactly(Stream stream, int count)
    {
        byte[] buffer = new byte[count];
        stream.ReadExactly(buffer);
        return buffer;
    }
}
