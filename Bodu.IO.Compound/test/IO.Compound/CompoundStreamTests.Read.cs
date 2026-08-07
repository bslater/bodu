// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
