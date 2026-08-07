// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbArrayDataSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the in-memory data source backing a buffered compound file.
/// </summary>
/// <remarks>
/// The buffered reader reaches this source through <c>GetSpan</c> alone, so the copying <c>Read</c> members are
/// exercised only when a caller asks for bytes rather than a view. They are part of the abstraction every source
/// implements, and the streaming source's equivalents behave differently enough that this one is worth pinning
/// directly.
/// </remarks>
[TestClass]
public class CfbArrayDataSourceTests
{
    /// <summary>
    /// Builds a 16-byte buffer whose bytes ascend from zero.
    /// </summary>
    /// <returns>The test buffer.</returns>
    private static byte[] Ascending()
    {
        byte[] data = new byte[16];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)i;

        return data;
    }

    /// <summary>
    /// Verifies that the source reports the length of the array it wraps.
    /// </summary>
    [TestMethod]
    public void Length_ShouldMatchBackingArray()
    {
        using var source = new CfbArrayDataSource(Ascending());

        Assert.AreEqual(16L, source.Length);
    }

    /// <summary>
    /// Verifies that <c>GetSpan</c> returns a view over the backing array rather than a copy, leaving the scratch
    /// buffer untouched - this zero-copy behavior is why the buffered reader prefers it.
    /// </summary>
    [TestMethod]
    public void GetSpan_WhenRangeRequested_ShouldViewBackingArrayWithoutUsingScratch()
    {
        byte[] data = Ascending();
        using var source = new CfbArrayDataSource(data);
        Span<byte> scratch = stackalloc byte[4];

        ReadOnlySpan<byte> span = source.GetSpan(4, 4, scratch);

        Assert.AreEqual(4, span.Length);
        Assert.AreEqual(4, span[0]);
        Assert.AreEqual(7, span[3]);
        Assert.IsTrue(scratch.TrimStart((byte)0).IsEmpty, "The scratch buffer should not have been written.");
    }

    /// <summary>
    /// Verifies that <c>Read</c> copies the requested range into the destination, sized by the destination rather
    /// than by an explicit count.
    /// </summary>
    [TestMethod]
    public void Read_WhenDestinationSupplied_ShouldCopyThatManyBytesFromOffset()
    {
        using var source = new CfbArrayDataSource(Ascending());
        byte[] destination = new byte[5];

        source.Read(10, destination);

        CollectionAssert.AreEqual(new byte[] { 10, 11, 12, 13, 14 }, destination);
    }

    /// <summary>
    /// Verifies that the asynchronous read copies the same bytes as the synchronous one and completes without
    /// yielding, since the content is already in memory.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task ReadAsync_WhenDestinationSupplied_ShouldCopySameBytesSynchronously()
    {
        using var source = new CfbArrayDataSource(Ascending());
        byte[] destination = new byte[5];

        ValueTask read = source.ReadAsync(10, destination, CancellationToken.None);

        Assert.IsTrue(read.IsCompletedSuccessfully);
        await read;
        CollectionAssert.AreEqual(new byte[] { 10, 11, 12, 13, 14 }, destination);
    }

    /// <summary>
    /// Verifies that a pre-canceled token cancels the read rather than copying, so a cancelled streaming read and a
    /// cancelled in-memory read behave alike from the caller's side.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task ReadAsync_WhenTokenIsAlreadyCanceled_ShouldCancelWithoutCopying()
    {
        using var source = new CfbArrayDataSource(Ascending());
        byte[] destination = new byte[5];
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await source.ReadAsync(10, destination, cts.Token);
        });

        CollectionAssert.AreEqual(new byte[5], destination);
    }
}
