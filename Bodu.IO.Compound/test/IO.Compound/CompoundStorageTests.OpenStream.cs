// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.OpenStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies how <see cref="CompoundStorage.OpenStream(string, FileMode, FileAccess)" /> translates the two
/// recoverable-absence results of its Try-core into exceptions.
/// </summary>
/// <remarks>
/// The Try-core reports <see langword="false" /> for both "the stream does not exist" and
/// "<see cref="FileMode.CreateNew" /> and it does" - two different failures behind one result. The throwing overload
/// has to tell them apart after the fact, so these tests pin that each maps to its own exception type rather than
/// collapsing to a single one.
/// </remarks>
public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that <see cref="FileMode.CreateNew" /> over an existing staged stream throws
    /// <see cref="IOException" /> - the BCL's signal for "the file is already there" - rather than the
    /// not-found exception.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenCreateNewAndStreamExists_ShouldThrowIOException()
    {
        using CompoundFile file = CreateWritableWithStream(1, 2, 3);

        _ = Assert.ThrowsExactly<IOException>(() =>
        {
            using CompoundStream _ = file.RootStorage.OpenStream("Stream 1", FileMode.CreateNew, FileAccess.Write);
        });
    }

    /// <summary>
    /// Verifies that opening an absent staged stream throws <see cref="CompoundStreamNotFoundException" /> naming the
    /// stream, so the caller can report which entry was missing.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenOpenAndStreamIsAbsent_ShouldThrowStreamNotFound()
    {
        using var file = CompoundFile.Create(new MemoryStream());

        var ex = Assert.ThrowsExactly<CompoundStreamNotFoundException>(() =>
        {
            using CompoundStream _ = file.RootStorage.OpenStream("Missing", FileMode.Open, FileAccess.Write);
        });

        Assert.IsTrue(ex.Message.Contains("Missing", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a read-only request for an absent staged stream throws the not-found exception rather than the
    /// <see cref="IOException" /> reserved for the create-new collision.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenAccessIsReadAndStreamIsAbsent_ShouldThrowStreamNotFound()
    {
        using var file = CompoundFile.Create(new MemoryStream());

        _ = Assert.ThrowsExactly<CompoundStreamNotFoundException>(() =>
        {
            using CompoundStream _ = file.RootStorage.OpenStream("Missing", FileMode.Open, FileAccess.Read);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.TryOpenStorage(string, out CompoundStorage)" /> resolves a staged
    /// child storage on a writable file, and reports <see langword="false" /> for an absent one.
    /// </summary>
    [TestMethod]
    public void TryOpenStorage_WhenFileIsWritable_ShouldResolveStagedChildStorage()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        _ = file.RootStorage.CreateStorage("Storage 1");

        Assert.IsTrue(file.RootStorage.TryOpenStorage("Storage 1", out CompoundStorage? storage));
        Assert.AreEqual("Storage 1", storage!.Name);

        Assert.IsFalse(file.RootStorage.TryOpenStorage("Storage 2", out CompoundStorage? missing));
        Assert.IsNull(missing);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.EnumerateStorages" /> walks the staging tree on a writable file,
    /// returning only storages and not the sibling stream.
    /// </summary>
    [TestMethod]
    public void EnumerateStorages_WhenFileIsWritable_ShouldReturnStagedStoragesOnly()
    {
        using var file = CompoundFile.Create(new MemoryStream());
        _ = file.RootStorage.CreateStorage("Storage 1");
        _ = file.RootStorage.CreateStorage("Storage 2");
        using (CompoundStream seed = file.RootStorage.OpenStream("Stream 1", FileMode.Create, FileAccess.Write))
            seed.Write(new byte[] { 1 });

        var names = file.RootStorage.EnumerateStorages().Select(s => s.Name).ToList();

        CollectionAssert.AreEquivalent(new[] { "Storage 1", "Storage 2" }, names);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.EnumerateStreams" /> walks the staging tree on a writable file,
    /// reporting each staged stream's length from the staging node rather than from a committed container.
    /// </summary>
    [TestMethod]
    public void EnumerateStreams_WhenFileIsWritable_ShouldReturnStagedStreamsWithLengths()
    {
        using CompoundFile file = CreateWritableWithStream(1, 2, 3);
        _ = file.RootStorage.CreateStorage("Storage 1");

        var streams = file.RootStorage.EnumerateStreams().ToList();

        Assert.HasCount(1, streams);
        Assert.AreEqual("Stream 1", streams[0].Name);
        Assert.AreEqual(3L, streams[0].Length);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Minimal" /> reads as zeros the bytes of a stream whose sector
    /// lies wholly past the end of the file, and returns the stream at its declared size, through either read strategy.
    /// </summary>
    /// <param name="strategy">The read strategy the file is opened with.</param>
    [TestMethod]
    [DataRow(CompoundReadStrategy.Buffered)]
    [DataRow(CompoundReadStrategy.Streaming)]
    public void OpenStream_WhenSectorLiesPastTheEndOfTheFileAtMinimal_ShouldReadItAsZeros(CompoundReadStrategy strategy)
    {
        byte[] container = BuildStreamPastTheEnd(out byte[] expected);
        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Minimal, ReadStrategy = strategy };

        using var file = CompoundFile.Open(new MemoryStream(container), options);
        using CompoundStream stream = file.RootStorage.OpenStream("A");

        CollectionAssert.AreEqual(expected, stream.ReadAllBytes());
        CollectionAssert.AreEqual(expected, ReadInChunks(stream));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Strict" /> and <see cref="CompoundValidationLevel.Compatible" />
    /// reject a stream whose sector lies wholly past the end of the file with
    /// <see cref="CompoundFileError.SectorOutOfRange" />, through either read strategy.
    /// </summary>
    /// <param name="strategy">The read strategy the file is opened with.</param>
    [TestMethod]
    [DataRow(CompoundReadStrategy.Buffered)]
    [DataRow(CompoundReadStrategy.Streaming)]
    public void OpenStream_WhenSectorLiesPastTheEndOfTheFile_ShouldThrowSectorOutOfRange(CompoundReadStrategy strategy)
    {
        byte[] container = BuildStreamPastTheEnd(out _);

        foreach (CompoundValidationLevel level in new[] { CompoundValidationLevel.Strict, CompoundValidationLevel.Compatible })
        {
            var options = new CompoundFileOptions { ValidationLevel = level, ReadStrategy = strategy };

            // Strict rejects the FAT entry that allocates the sector past the end of the file as the file opens.
            CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
            {
                using var file = CompoundFile.Open(new MemoryStream(container), options);
                using CompoundStream stream = file.RootStorage.OpenStream("A");
                _ = ReadInChunks(stream);
            }, $"{level}");

            Assert.AreEqual(CompoundFileError.SectorOutOfRange, ex.Category, $"{level}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Strict" /> and <see cref="CompoundValidationLevel.Compatible" />
    /// reject, when it is opened, a stream whose declared size exceeds what its sector chain holds, with
    /// <see cref="CompoundFileError.StreamChainTooShort" />, through either read strategy.
    /// </summary>
    /// <param name="strategy">The read strategy the file is opened with.</param>
    [TestMethod]
    [DataRow(CompoundReadStrategy.Buffered)]
    [DataRow(CompoundReadStrategy.Streaming)]
    public void OpenStream_WhenDeclaredSizeExceedsTheChain_ShouldThrowStreamChainTooShort(CompoundReadStrategy strategy)
    {
        byte[] container = BuildOverDeclaredStream(out _);

        foreach (CompoundValidationLevel level in new[] { CompoundValidationLevel.Strict, CompoundValidationLevel.Compatible })
        {
            var options = new CompoundFileOptions { ValidationLevel = level, ReadStrategy = strategy };
            using var file = CompoundFile.Open(new MemoryStream(container), options);

            CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
            {
                using CompoundStream stream = file.RootStorage.OpenStream("A");
            }, $"{level}");

            Assert.AreEqual(CompoundFileError.StreamChainTooShort, ex.Category, $"{level}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Minimal" /> zero-pads a stream whose declared size exceeds what
    /// its sector chain holds to that size, through either read strategy.
    /// </summary>
    /// <param name="strategy">The read strategy the file is opened with.</param>
    /// <param name="emptyChain">Whether the stream's starting sector lies outside the FAT, so its chain holds nothing.</param>
    [TestMethod]
    [DataRow(CompoundReadStrategy.Buffered, false)]
    [DataRow(CompoundReadStrategy.Streaming, false)]
    [DataRow(CompoundReadStrategy.Buffered, true)]
    [DataRow(CompoundReadStrategy.Streaming, true)]
    public void OpenStream_WhenDeclaredSizeExceedsTheChainAtMinimal_ShouldZeroPadTheStream(CompoundReadStrategy strategy, bool emptyChain)
    {
        byte[] container = BuildOverDeclaredStream(out byte[] expected);
        if (emptyChain)
        {
            CompoundContainerBytes.WriteUInt32(container, CompoundContainerBytes.FindEntryOffset(container, "A") + CompoundContainerBytes.StartSectorOffset, 0x00FFFFFF);
            expected = new byte[expected.Length];
        }

        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Minimal, ReadStrategy = strategy };

        using var file = CompoundFile.Open(new MemoryStream(container), options);
        using CompoundStream stream = file.RootStorage.OpenStream("A");

        Assert.AreEqual(expected.Length, stream.Length);
        CollectionAssert.AreEqual(expected, stream.ReadAllBytes());
        CollectionAssert.AreEqual(expected, ReadInChunks(stream));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.OpenStream(string)" /> throws <see cref="ObjectDisposedException" />
    /// once a writable file has been disposed, as it does for a read-only file.
    /// </summary>
    [TestMethod]
    public void OpenStream_WhenWritableFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        CompoundStorage root = CreateDisposedWritableRoot();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            using CompoundStream stream = root.OpenStream("A");
        });
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.OpenStream(string, FileMode, FileAccess)" /> throws
    /// <see cref="ObjectDisposedException" /> once a writable file has been disposed, whether the call reads, opens for
    /// writing, or creates, and stages nothing.
    /// </summary>
    /// <param name="name">The stream name passed to the call.</param>
    /// <param name="mode">The file mode passed to the call.</param>
    /// <param name="access">The access passed to the call.</param>
    [TestMethod]
    [DataRow("A", FileMode.Open, FileAccess.Read)]
    [DataRow("A", FileMode.Open, FileAccess.ReadWrite)]
    [DataRow("New", FileMode.Create, FileAccess.Write)]
    [DataRow("New", FileMode.Append, FileAccess.Write)]
    public void OpenStream_WhenWritableFileIsDisposed_ForFileMode_ShouldThrowObjectDisposedException(string name, FileMode mode, FileAccess access)
    {
        CompoundStorage root = CreateDisposedWritableRoot();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            using CompoundStream stream = root.OpenStream(name, mode, access);
        });

        AssertDisposedRootUnchanged(root);
    }

    /// <summary>
    /// Builds a container whose 5000-byte stream <c>A</c> has its last sector moved to the end of the file and then cut
    /// off, so that sector lies wholly past the end of the file.
    /// </summary>
    /// <param name="expected">Receives A's content with the bytes of that sector read as zeros.</param>
    /// <returns>The container.</returns>
    private static byte[] BuildStreamPastTheEnd(out byte[] expected)
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", CompoundContainerBytes.Pattern(5000));
        byte[] container = builder.ToArray();
        int entry = CompoundContainerBytes.FindEntryOffset(container, "A");

        expected = CompoundContainerBytes.Pattern(5000);
        expected.AsSpan(4608).Clear();
        return CompoundContainerBytes.MoveLastSector(container, entry + CompoundContainerBytes.StartSectorOffset)[..container.Length];
    }

    /// <summary>
    /// Builds a container whose stream <c>A</c>, 5000 bytes in ten sectors, declares a size of 9000.
    /// </summary>
    /// <param name="expected">Receives A's content zero-padded to 9000 bytes.</param>
    /// <returns>The container.</returns>
    private static byte[] BuildOverDeclaredStream(out byte[] expected)
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", CompoundContainerBytes.Pattern(5000));
        byte[] container = builder.ToArray();
        int entry = CompoundContainerBytes.FindEntryOffset(container, "A");
        CompoundContainerBytes.WriteInt64(container, entry + CompoundContainerBytes.SizeOffset, 9000);

        // The chain's ten sectors hold 5120 bytes: the 5000 written and the zeros that pad the last sector.
        expected = new byte[9000];
        CompoundContainerBytes.Pattern(5000).CopyTo(expected, 0);
        return container;
    }

    /// <summary>
    /// Reads a stream from its start through <see cref="Stream.Read(Span{byte})" />, 1000 bytes at a time.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <returns>The bytes read.</returns>
    private static byte[] ReadInChunks(Stream stream)
    {
        stream.Position = 0;
        using var copy = new MemoryStream();
        byte[] buffer = new byte[1000];
        int read;
        while ((read = stream.Read(buffer.AsSpan())) > 0)
            copy.Write(buffer, 0, read);

        return copy.ToArray();
    }
}
