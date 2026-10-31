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
public partial class CompoundStreamTests
{
    /// <summary>The length, in bytes, from which a stream is stored in regular sectors rather than the mini stream.</summary>
    private const int MiniStreamCutoff = 4096;

    /// <summary>The length of the stream the cutoff tests commit beside the edited one and never edit.</summary>
    private const int BystanderLength = 200;

    /// <summary>
    /// The offset into the catalog pattern at which the bytes an edit writes begin, so that they differ at every
    /// position from the bytes first committed.
    /// </summary>
    private const long EditedPatternStart = 101;

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

        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("Data", payload);
        return builder.ToArray();
    }

    /// <summary>
    /// Creates a writable compound file, opens a read-write cursor over its stream <c>Data</c> holding the bytes 1, 2 and
    /// 3, and then disposes the file while the cursor stays open.
    /// </summary>
    /// <returns>The open cursor, whose owning file has been disposed; dispose it when finished.</returns>
    private static CompoundStream OpenCursorOverDisposedFile()
    {
        var file = CompoundFile.Create(new MemoryStream());
        file.RootStorage.CreateStream("Data", new byte[] { 1, 2, 3 });
        CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Open, FileAccess.ReadWrite);
        file.Dispose();
        return cursor;
    }

    /// <summary>
    /// Returns the bytes of the stream <c>Other</c>, which the cutoff tests commit beside <c>Data</c> and never edit.
    /// </summary>
    /// <returns>The catalog pattern from offset 50, <see cref="BystanderLength" /> bytes long.</returns>
    private static byte[] BystanderBytes() =>
        CompoundContainerBytes.Pattern(BystanderLength, start: 50);

    /// <summary>
    /// Creates and commits a compound file holding the stream <c>Data</c>, carrying the catalog pattern at the given
    /// length, beside the untouched stream <c>Other</c>.
    /// </summary>
    /// <param name="dataLength">The length of <c>Data</c>, in bytes.</param>
    /// <returns>A stream over the committed bytes, positioned at the start, for the edit to reopen.</returns>
    private static MemoryStream CreateCommittedContainer(int dataLength)
    {
        var destination = new MemoryStream();
        using (var file = CompoundFile.Create(destination, leaveOpen: true))
        {
            file.RootStorage.CreateStream("Data", CompoundContainerBytes.Pattern(dataLength));
            file.RootStorage.CreateStream("Other", BystanderBytes());
            file.Commit();
        }

        destination.Position = 0;
        return destination;
    }

    /// <summary>
    /// Reopens committed bytes read-only at <see cref="CompoundValidationLevel.Strict" />, so that a container the
    /// edit laid out wrongly is rejected rather than tolerated, and asserts that <c>Data</c> holds exactly the
    /// expected bytes and that <c>Other</c> holds the bytes it was committed with.
    /// </summary>
    /// <param name="container">The committed bytes.</param>
    /// <param name="expectedData">The bytes <c>Data</c> is expected to hold.</param>
    private static void AssertCommittedStreams(byte[] container, byte[] expectedData)
    {
        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };
        using var reopened = CompoundFile.Open(new MemoryStream(container, writable: false), options);

        using CompoundStream data = reopened.RootStorage.OpenStream("Data");
        Assert.AreEqual(expectedData.Length, data.Length);
        CollectionAssert.AreEqual(expectedData, data.ReadAllBytes());

        using CompoundStream other = reopened.RootStorage.OpenStream("Other");
        CollectionAssert.AreEqual(BystanderBytes(), other.ReadAllBytes());
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
        using var file = CompoundFile.Open(new MemoryStream(bytes), buffered: buffered);
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
        using var file = CompoundFile.Open(new MemoryStream(bytes), buffered: buffered);
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
        using var file = CompoundFile.Open(new MemoryStream(bytes));
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
        using var file = CompoundFile.Open(new MemoryStream(bytes));
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        stream.Position = payload.Length;

        Assert.AreEqual(0, stream.Read(new byte[16].AsSpan()));
    }
}
