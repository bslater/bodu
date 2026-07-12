// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.StreamingReadAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Reads a stream cursor to its end asynchronously in odd-sized chunks.
    /// </summary>
    /// <param name="stream">The cursor to drain.</param>
    /// <returns>The bytes read.</returns>
    private static async Task<byte[]> ReadAllAsync(CompoundStream stream)
    {
        using MemoryStream sink = new();
        byte[] chunk = new byte[7];
        int read;
        while ((read = await stream.ReadAsync(chunk.AsMemory())) > 0)
            sink.Write(chunk, 0, read);

        return sink.ToArray();
    }

    /// <summary>
    /// Verifies that a streaming-mode cursor read asynchronously in chunks returns the same bytes as the synchronous
    /// materialization.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WhenStreaming_ShouldMatchSyncRead()
    {
        using MemoryStream source = CompoundFixtures.OpenReference("valid/clean.dat");
        using var file = CompoundFile.Open(source, buffered: false);

        foreach ((CompoundStorage storage, CompoundEntryInfo info) in EnumerateAllStreams(file.RootStorage))
        {
            byte[] expected;
            using (CompoundStream sync = storage.OpenStream(info.Name))
                expected = sync.ReadAllBytes();

            using CompoundStream cursor = storage.OpenStream(info.Name);
            byte[] actual = await ReadAllAsync(cursor);

            CollectionAssert.AreEqual(expected, actual, info.Name);
        }
    }

    /// <summary>
    /// Verifies that reading a streaming-mode cursor with a canceled token throws
    /// <see cref="OperationCanceledException" />.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WhenStreamingAndCancelled_ShouldThrowOperationCanceledException()
    {
        using MemoryStream source = CompoundFixtures.OpenReference("valid/clean.dat");
        using var file = CompoundFile.Open(source, buffered: false);
        (CompoundStorage storage, CompoundEntryInfo info) = EnumerateAllStreams(file.RootStorage).First();

        using CompoundStream cursor = storage.OpenStream(info.Name);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        byte[] buffer = new byte[16];
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await cursor.ReadAsync(buffer.AsMemory(), cts.Token));
    }

    /// <summary>
    /// Verifies that two streaming cursors over the same source, read concurrently, each return the correct bytes —
    /// the seek-and-read serialization holds under interleaving.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WhenTwoCursorsReadConcurrently_ShouldReturnCorrectBytes()
    {
        using MemoryStream source = CompoundFixtures.OpenReference("valid/clean.dat");
        using var file = CompoundFile.Open(source, buffered: false);
        (CompoundStorage storage, CompoundEntryInfo info) = EnumerateAllStreams(file.RootStorage).First();

        byte[] expected;
        using (CompoundStream sync = storage.OpenStream(info.Name))
            expected = sync.ReadAllBytes();

        using CompoundStream cursorA = storage.OpenStream(info.Name);
        using CompoundStream cursorB = storage.OpenStream(info.Name);
        byte[][] results = await Task.WhenAll(ReadAllAsync(cursorA), ReadAllAsync(cursorB));

        CollectionAssert.AreEqual(expected, results[0]);
        CollectionAssert.AreEqual(expected, results[1]);
    }
}
