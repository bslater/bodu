// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.TryOpenStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the (<see cref="FileMode" />, <see cref="FileAccess" />) matrix of
/// <see cref="CompoundStorage.TryOpenStream(string, FileMode, FileAccess, out CompoundStream)" /> on a writable
/// compound file.
/// </summary>
/// <remarks>
/// <para>
/// On a writable file the call is served from the staging tree rather than the container, so every combination is
/// decided in memory. The two axes are not independent: a request without write access reads the staged payload and
/// never creates, so the mode is consulted only once write access is asked for.
/// </para>
/// <para>
/// The method reports <see langword="false" /> only for the two recoverable-absence cases - opening a stream that does
/// not exist, and <see cref="FileMode.CreateNew" /> over one that does. Every other rejection is an exception, which
/// is why <see cref="FileMode.Truncate" /> appears here rather than as a <see langword="false" /> result.
/// </para>
/// </remarks>
public partial class CompoundStorageTests
{
    /// <summary>
    /// Creates a writable compound file whose root holds a single stream named <c>Stream 1</c>.
    /// </summary>
    /// <param name="content">The payload staged into <c>Stream 1</c>.</param>
    /// <returns>The open writable file; dispose it when finished.</returns>
    private static CompoundFile CreateWritableWithStream(params byte[] content)
    {
        var file = CompoundFile.Create(new MemoryStream());
        using (CompoundStream seed = file.RootStorage.OpenStream("Stream 1", FileMode.Create, FileAccess.Write))
            seed.Write(content);

        return file;
    }

    /// <summary>
    /// Verifies that a read-only request over a writable file returns the staged payload without creating anything.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenAccessIsReadOnAWritableFile_ShouldReturnStagedPayload()
    {
        using CompoundFile file = CreateWritableWithStream(1, 2, 3);

        Assert.IsTrue(file.RootStorage.TryOpenStream("Stream 1", FileMode.Open, FileAccess.Read, out CompoundStream? stream));

        using (stream)
        {
            byte[] buffer = new byte[3];
            Assert.AreEqual(3, stream!.Read(buffer, 0, buffer.Length));
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, buffer);
        }
    }

    /// <summary>
    /// Verifies that a read-only request for an absent stream reports <see langword="false" /> and does not stage a
    /// new entry, whatever mode is named - the mode is consulted only once write access is requested.
    /// </summary>
    /// <param name="mode">The file mode named alongside read-only access.</param>
    [TestMethod]
    [DataRow(FileMode.Open)]
    [DataRow(FileMode.Create)]
    [DataRow(FileMode.OpenOrCreate)]
    public void TryOpenStream_WhenAccessIsReadAndStreamIsAbsent_ShouldReturnFalseWithoutCreating(FileMode mode)
    {
        using var file = CompoundFile.Create(new MemoryStream());

        Assert.IsFalse(file.RootStorage.TryOpenStream("Missing", mode, FileAccess.Read, out CompoundStream? stream));

        Assert.IsNull(stream);
        Assert.IsEmpty(file.RootStorage.EnumerateStreams());
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.CreateNew" /> over an existing stream reports <see langword="false" />
    /// rather than replacing the payload.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenCreateNewAndStreamExists_ShouldReturnFalseAndPreservePayload()
    {
        using CompoundFile file = CreateWritableWithStream(1, 2, 3);

        Assert.IsFalse(file.RootStorage.TryOpenStream("Stream 1", FileMode.CreateNew, FileAccess.Write, out CompoundStream? stream));

        Assert.IsNull(stream);
        using CompoundStream existing = file.RootStorage.OpenStream("Stream 1");
        Assert.AreEqual(3L, existing.Length);
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.CreateNew" /> for an absent stream stages a new empty entry.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenCreateNewAndStreamIsAbsent_ShouldStageAnEmptyStream()
    {
        using var file = CompoundFile.Create(new MemoryStream());

        Assert.IsTrue(file.RootStorage.TryOpenStream("Stream 1", FileMode.CreateNew, FileAccess.Write, out CompoundStream? stream));

        using (stream)
            Assert.AreEqual(0L, stream!.Length);

        Assert.HasCount(1, file.RootStorage.EnumerateStreams());
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.Create" /> for an absent stream stages a new empty entry, rather than
    /// reporting the absence.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenCreateAndStreamIsAbsent_ShouldStageAnEmptyStream()
    {
        using var file = CompoundFile.Create(new MemoryStream());

        Assert.IsTrue(file.RootStorage.TryOpenStream("Stream 1", FileMode.Create, FileAccess.Write, out CompoundStream? stream));

        using (stream)
            Assert.AreEqual(0L, stream!.Length);

        Assert.HasCount(1, file.RootStorage.EnumerateStreams());
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.Create" /> over an existing stream discards the previous payload, so the
    /// cursor starts empty rather than over the old bytes.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenCreateAndStreamExists_ShouldDiscardPreviousPayload()
    {
        using CompoundFile file = CreateWritableWithStream(1, 2, 3);

        Assert.IsTrue(file.RootStorage.TryOpenStream("Stream 1", FileMode.Create, FileAccess.Write, out CompoundStream? stream));

        using (stream)
            Assert.AreEqual(0L, stream!.Length);
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.Open" /> for an absent stream reports <see langword="false" /> even with
    /// write access, because Open never creates.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenOpenAndStreamIsAbsent_ShouldReturnFalseWithoutCreating()
    {
        using var file = CompoundFile.Create(new MemoryStream());

        Assert.IsFalse(file.RootStorage.TryOpenStream("Missing", FileMode.Open, FileAccess.Write, out CompoundStream? stream));

        Assert.IsNull(stream);
        Assert.IsEmpty(file.RootStorage.EnumerateStreams());
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.OpenOrCreate" /> seeds the cursor from an existing payload, leaving it
    /// intact where <see cref="FileMode.Create" /> would truncate.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenOpenOrCreateAndStreamExists_ShouldSeedFromExistingPayload()
    {
        using CompoundFile file = CreateWritableWithStream(1, 2, 3);

        Assert.IsTrue(file.RootStorage.TryOpenStream("Stream 1", FileMode.OpenOrCreate, FileAccess.ReadWrite, out CompoundStream? stream));

        using (stream)
        {
            Assert.AreEqual(3L, stream!.Length);
            Assert.AreEqual(0L, stream.Position);
        }
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.OpenOrCreate" /> for an absent stream stages a new empty entry.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenOpenOrCreateAndStreamIsAbsent_ShouldStageAnEmptyStream()
    {
        using var file = CompoundFile.Create(new MemoryStream());

        Assert.IsTrue(file.RootStorage.TryOpenStream("Stream 1", FileMode.OpenOrCreate, FileAccess.Write, out CompoundStream? stream));

        using (stream)
            Assert.AreEqual(0L, stream!.Length);

        Assert.HasCount(1, file.RootStorage.EnumerateStreams());
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.Append" /> over an existing stream positions the cursor at the end, so a
    /// write extends the payload rather than overwriting it.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenAppendAndStreamExists_ShouldPositionAtEnd()
    {
        using CompoundFile file = CreateWritableWithStream(1, 2, 3);

        Assert.IsTrue(file.RootStorage.TryOpenStream("Stream 1", FileMode.Append, FileAccess.Write, out CompoundStream? stream));

        using (stream)
        {
            Assert.AreEqual(3L, stream!.Position);
            stream.Write(new byte[] { 4 });
        }

        using CompoundStream reopened = file.RootStorage.OpenStream("Stream 1");
        byte[] buffer = new byte[4];
        Assert.AreEqual(4, reopened.Read(buffer, 0, buffer.Length));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, buffer);
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.Append" /> for an absent stream stages a new empty entry positioned at zero.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenAppendAndStreamIsAbsent_ShouldStageAnEmptyStreamAtZero()
    {
        using var file = CompoundFile.Create(new MemoryStream());

        Assert.IsTrue(file.RootStorage.TryOpenStream("Stream 1", FileMode.Append, FileAccess.Write, out CompoundStream? stream));

        using (stream)
        {
            Assert.AreEqual(0L, stream!.Length);
            Assert.AreEqual(0L, stream.Position);
        }
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.Truncate" /> is rejected with <see cref="NotSupportedException" /> rather
    /// than silently falling through to another mode's behavior.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenModeIsTruncate_ShouldThrowNotSupportedException()
    {
        using CompoundFile file = CreateWritableWithStream(1, 2, 3);

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = file.RootStorage.TryOpenStream("Stream 1", FileMode.Truncate, FileAccess.Write, out _);
        });
    }

    /// <summary>
    /// Verifies that a write request on a read-only compound file is rejected with
    /// <see cref="NotSupportedException" />, whatever mode is named.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenWriteAccessRequestedOnReadOnlyFile_ShouldThrowNotSupportedException()
    {
        using CompoundFile file = OpenNested();

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = file.RootStorage.TryOpenStream("Stream 1", FileMode.Open, FileAccess.Write, out _);
        });
    }
}
