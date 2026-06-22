// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the span- and async-based read surfaces of <see cref="CompoundStream" /> over both the buffered and
/// streaming backings.
/// </summary>
[TestClass]
public class CompoundStreamTests
{
    /// <summary>
    /// Builds a single-stream container whose <c>Data</c> stream carries a deterministic multi-sector payload.
    /// </summary>
    /// <param name="payload">Receives the payload written to the stream.</param>
    /// <returns>The serialized compound-file bytes.</returns>
    private static byte[] BuildContainer(out byte[] payload)
    {
        payload = new byte[9000];
        for (int i = 0; i < payload.Length; i++)
            payload[i] = (byte)((i * 7) + 3);

        var builder = new CompoundFileBuilder();
        _ = builder.Root.AddStream("Data", payload);
        return builder.ToArray();
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStream.Read(Span{byte})" /> returns the full payload for both backings.
    /// </summary>
    /// <param name="buffered">Whether the owning compound file buffers the whole source in memory.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ReadSpan_WhenReadInChunks_ShouldReturnFullPayload(bool buffered)
    {
        byte[] bytes = BuildContainer(out byte[] payload);
        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes), buffered: buffered);
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        byte[] result = new byte[payload.Length];
        int offset = 0;
        int read;
        while (offset < result.Length && (read = stream.Read(result.AsSpan(offset, Math.Min(1000, result.Length - offset)))) > 0)
            offset += read;

        Assert.AreEqual(payload.Length, offset);
        CollectionAssert.AreEqual(payload, result);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStream.ReadAsync(Memory{byte}, System.Threading.CancellationToken)" /> reads the
    /// full payload for both backings.
    /// </summary>
    /// <param name="buffered">Whether the owning compound file buffers the whole source in memory.</param>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ReadAsyncMemory_WhenReadInChunks_ShouldReturnFullPayload(bool buffered)
    {
        byte[] bytes = BuildContainer(out byte[] payload);
        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes), buffered: buffered);
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        byte[] result = new byte[payload.Length];
        int offset = 0;
        int read;
        while (offset < result.Length &&
            (read = await stream.ReadAsync(result.AsMemory(offset, Math.Min(1000, result.Length - offset)))) > 0)
        {
            offset += read;
        }

        Assert.AreEqual(payload.Length, offset);
        CollectionAssert.AreEqual(payload, result);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStream.ReadAsync(Memory{byte}, System.Threading.CancellationToken)" /> returns a
    /// canceled result when invoked with an already-canceled token.
    /// </summary>
    [TestMethod]
    public void ReadAsyncMemory_WhenTokenCanceled_ShouldReturnCanceled()
    {
        byte[] bytes = BuildContainer(out _);
        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes));
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        ValueTask<int> read = stream.ReadAsync(new byte[16].AsMemory(), cts.Token);

        Assert.IsTrue(read.IsCanceled);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStream.Read(Span{byte})" /> returns zero once the stream is exhausted.
    /// </summary>
    [TestMethod]
    public void ReadSpan_WhenAtEnd_ShouldReturnZero()
    {
        byte[] bytes = BuildContainer(out byte[] payload);
        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes));
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        stream.Position = payload.Length;

        Assert.AreEqual(0, stream.Read(new byte[16].AsSpan()));
    }
}
