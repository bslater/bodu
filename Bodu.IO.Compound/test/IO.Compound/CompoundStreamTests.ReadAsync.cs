// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamTests.ReadAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the array-based <see cref="CompoundStream.ReadAsync(byte[], int, int, CancellationToken)" /> overload and
/// the cancellation contract shared by both read overloads.
/// </summary>
/// <remarks>
/// The array overload delegates to the <see cref="Memory{T}" /> one, so its own job is argument validation and the
/// offset/count slice. Both are asserted here because a delegating overload that mis-slices is invisible from the
/// overload it forwards to.
/// </remarks>
public partial class CompoundStreamTests
{
    /// <summary>
    /// Verifies that the array overload reads the full payload in chunks for both backings, matching the
    /// <see cref="Memory{T}" /> overload it delegates to.
    /// </summary>
    /// <param name="buffered">Whether the owning compound file buffers the whole source in memory.</param>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ReadAsyncArray_WhenReadInChunks_ShouldReturnFullPayload(bool buffered)
    {
        byte[] bytes = BuildContainer(out byte[] payload);
        using var file = CompoundFile.Open(new MemoryStream(bytes), buffered: buffered);
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        byte[] result = new byte[payload.Length];
        int offset = 0;
        int read;
        while (offset < result.Length &&
            (read = await stream.ReadAsync(result, offset, Math.Min(1000, result.Length - offset))) > 0)
        {
            offset += read;
        }

        Assert.AreEqual(payload.Length, offset);
        CollectionAssert.AreEqual(payload, result);
    }

    /// <summary>
    /// Verifies that the array overload honors the requested offset and count rather than filling the buffer from
    /// zero, so the bytes land where the caller asked for them.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task ReadAsyncArray_WhenOffsetIsNonZero_ShouldWriteAtThatOffset()
    {
        byte[] bytes = BuildContainer(out byte[] payload);
        using var file = CompoundFile.Open(new MemoryStream(bytes));
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        byte[] result = new byte[10];
        int read = await stream.ReadAsync(result, 4, 4);

        Assert.AreEqual(4, read);
        Assert.AreEqual(0, result[0]);
        Assert.AreEqual(0, result[3]);
        CollectionAssert.AreEqual(payload.Take(4).ToArray(), result.Skip(4).Take(4).ToArray());
        Assert.AreEqual(0, result[8]);
    }

    /// <summary>
    /// Verifies that the array overload rejects a null buffer before touching the cursor.
    /// </summary>
    [TestMethod]
    public void ReadAsyncArray_WhenBufferIsNull_ShouldThrowArgumentNullException()
    {
        byte[] bytes = BuildContainer(out _);
        using var file = CompoundFile.Open(new MemoryStream(bytes));
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = stream.ReadAsync(null!, 0, 1);
        });
    }

    /// <summary>
    /// Verifies that the array overload rejects an offset and count that do not fit the buffer.
    /// </summary>
    [TestMethod]
    public void ReadAsyncArray_WhenOffsetAndCountExceedBuffer_ShouldThrowArgumentException()
    {
        byte[] bytes = BuildContainer(out _);
        using var file = CompoundFile.Open(new MemoryStream(bytes));
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = stream.ReadAsync(new byte[4], 2, 4);
        });
    }

    /// <summary>
    /// Verifies that a read past the end of the payload reports zero bytes rather than throwing, for both backings.
    /// </summary>
    /// <param name="buffered">Whether the owning compound file buffers the whole source in memory.</param>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ReadAsync_WhenPositionedAtEnd_ShouldReturnZero(bool buffered)
    {
        byte[] bytes = BuildContainer(out byte[] payload);
        using var file = CompoundFile.Open(new MemoryStream(bytes), buffered: buffered);
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        _ = stream.Seek(payload.Length, SeekOrigin.Begin);

        Assert.AreEqual(0, await stream.ReadAsync(new byte[8].AsMemory()));
    }
}
