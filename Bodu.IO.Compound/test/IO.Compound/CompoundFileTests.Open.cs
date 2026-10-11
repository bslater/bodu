// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that opening a <see langword="null" /> stream throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CompoundFile.Open(null!);
        });

        Assert.AreEqual("stream", ex.ParamName);
    }

    /// <summary>
    /// Verifies that opening data whose leading bytes are not the compound-file signature throws
    /// <see cref="CompoundFileFormatException" /> with the <see cref="CompoundFileError.InvalidSignature" /> category.
    /// </summary>
    [TestMethod]
    public void Open_WhenSignatureIsInvalid_ShouldThrowWithInvalidSignatureCategory()
    {
        using MemoryStream stream = new(new byte[600]);

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundFile.Open(stream);
        });

        Assert.AreEqual(CompoundFileError.InvalidSignature, ex.Category);
    }

    /// <summary>
    /// Verifies that opening data shorter than a header throws <see cref="CompoundFileFormatException" /> with the
    /// <see cref="CompoundFileError.TruncatedFile" /> category.
    /// </summary>
    [TestMethod]
    public void Open_WhenDataIsTooShort_ShouldThrowWithTruncatedFileCategory()
    {
        using MemoryStream stream = new(new byte[64]);

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundFile.Open(stream);
        });

        Assert.AreEqual(CompoundFileError.TruncatedFile, ex.Category);
    }

    /// <summary>
    /// Verifies that requesting an unsupported file mode throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenModeIsUnsupported_ShouldThrowNotSupportedException()
    {
        using MemoryStream stream = CompoundFixtures.OpenStream(CompoundFixtures.SampleCompound);

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            using var file = CompoundFile.Open(stream, FileMode.Append, FileAccess.Write);
        });
    }

    /// <summary>
    /// Verifies that the opened file reports read access and is not writable.
    /// </summary>
    [TestMethod]
    public void Open_WhenOpenedForRead_ShouldReportReadAccess()
    {
        using CompoundFile file = OpenSample();

        Assert.AreEqual(FileAccess.Read, file.Access);
        Assert.IsTrue(file.CanRead);
        Assert.IsFalse(file.CanWrite);
    }

    /// <summary>
    /// Verifies that disposing with <c>leaveOpen</c> set keeps the source stream usable afterwards.
    /// </summary>
    [TestMethod]
    public void Open_WhenLeaveOpenIsTrue_ShouldNotDisposeSourceStream()
    {
        using MemoryStream source = CompoundFixtures.OpenStream(CompoundFixtures.SampleCompound);

        using (var file = CompoundFile.Open(source, leaveOpen: true))
        {
            Assert.IsNotEmpty(file.RootStorage.EnumerateEntries());
        }

        Assert.IsTrue(source.CanRead);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.OpenRead(string)" /> opens a compound file from a path for reading.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenPath_ShouldReadContainer()
    {
        string path = Path.Combine(Path.GetTempPath(), $"bodu-openread-{Guid.NewGuid():N}.cfb");
        try
        {
            using (MemoryStream source = CompoundFixtures.OpenStream(CompoundFixtures.SampleCompound))
                File.WriteAllBytes(path, source.ToArray());

            using var file = CompoundFile.OpenRead(path);

            Assert.AreEqual(FileAccess.Read, file.Access);
            Assert.IsNotEmpty(file.RootStorage.EnumerateEntries());
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.OpenRead(string)" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> path.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenPathIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => CompoundFile.OpenRead(null!));
    }

    /// <summary>
    /// Verifies that the path-based <see cref="CompoundFile.Open(string, FileMode, FileAccess, FileShare)" /> opens an
    /// existing file for reading.
    /// </summary>
    [TestMethod]
    public void Open_WhenPathOpenedForRead_ShouldReadContainer()
    {
        string path = Path.Combine(Path.GetTempPath(), $"bodu-openpath-{Guid.NewGuid():N}.cfb");
        try
        {
            using (MemoryStream source = CompoundFixtures.OpenStream(CompoundFixtures.SampleCompound))
                File.WriteAllBytes(path, source.ToArray());

            using var file = CompoundFile.Open(path, FileMode.Open, FileAccess.Read);

            Assert.AreEqual(FileAccess.Read, file.Access);
            Assert.IsNotEmpty(file.RootStorage.EnumerateEntries());
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that the path-based <see cref="CompoundFile.Open(string, FileMode, FileAccess, FileShare)" /> creates a
    /// new file, and that committed content is read back on reopen.
    /// </summary>
    [TestMethod]
    public void Open_WhenPathCreatedForWrite_ShouldRoundTripThroughReopen()
    {
        string path = Path.Combine(Path.GetTempPath(), $"bodu-createpath-{Guid.NewGuid():N}.cfb");
        byte[] payload = [1, 2, 3, 4, 5];
        try
        {
            using (var file = CompoundFile.Open(path, FileMode.Create, FileAccess.ReadWrite))
            {
                using (CompoundStream stream = file.RootStorage.CreateStream("Data"))
                    stream.Write(payload, 0, payload.Length);

                file.Commit();
            }

            using var reopened = CompoundFile.Open(path, FileMode.Open, FileAccess.Read);
            using CompoundStream read = reopened.RootStorage.OpenStream("Data");

            CollectionAssert.AreEqual(payload, read.ReadAllBytes());
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that the path-based <see cref="CompoundFile.Open(string, FileMode, FileAccess, FileShare)" /> updates an
    /// existing file in place: a stream added under update mode survives commit and reopen.
    /// </summary>
    [TestMethod]
    public void Open_WhenPathUpdatedForWrite_ShouldPersistEdits()
    {
        string path = Path.Combine(Path.GetTempPath(), $"bodu-updatepath-{Guid.NewGuid():N}.cfb");
        try
        {
            using (var file = CompoundFile.Open(path, FileMode.Create, FileAccess.ReadWrite))
            {
                file.RootStorage.CreateStream("Original", new byte[] { 9 });
                file.Commit();
            }

            using (var file = CompoundFile.Open(path, FileMode.Open, FileAccess.ReadWrite))
            {
                file.RootStorage.CreateStream("Added", new byte[] { 8 });
                file.Commit();
            }

            using var reopened = CompoundFile.Open(path, FileMode.Open, FileAccess.Read);
            Assert.IsTrue(reopened.RootStorage.TryOpenStream("Original", out _));
            Assert.IsTrue(reopened.RootStorage.TryOpenStream("Added", out _));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that the path-based <see cref="CompoundFile.Open(string, FileMode, FileAccess, FileShare)" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> path.
    /// </summary>
    [TestMethod]
    public void Open_WhenPathIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            CompoundFile.Open((string)null!, FileMode.Open, FileAccess.Read));
    }

    /// <summary>
    /// Verifies that a path overload that opened the file but then failed to parse it closes the handle before
    /// propagating, so the file can be deleted immediately afterwards.
    /// </summary>
    /// <remarks>
    /// Each path overload opens a <see cref="FileStream" /> and hands it to a core that may throw. The stream is the
    /// overload's to own until the core takes it, so the failure path has to dispose it - a leak here would surface
    /// much later as a locked file, far from the malformed input that caused it. Deleting the file inside the test is
    /// the assertion: on Windows it fails outright while a handle is open.
    /// </remarks>
    /// <param name="overload">The path overload under test.</param>
    [TestMethod]
    [DataRow("OpenRead")]
    [DataRow("OpenReadWithOptions")]
    [DataRow("OpenModeAccess")]
    public void PathOverloads_WhenContentIsNotACompoundFile_ShouldCloseTheFileBeforeThrowing(string overload)
    {
        string path = Path.Combine(Path.GetTempPath(), $"bodu-cfb-{Guid.NewGuid():N}.cfb");
        File.WriteAllBytes(path, "not a compound file"u8.ToArray());

        try
        {
            _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
            {
                using CompoundFile _ = overload switch
                {
                    "OpenRead" => CompoundFile.OpenRead(path),
                    "OpenReadWithOptions" => CompoundFile.OpenRead(path, new CompoundFileOptions()),
                    _ => CompoundFile.Open(path, FileMode.Open, FileAccess.Read),
                };
            });

            File.Delete(path);
            Assert.IsFalse(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.Create(string, CompoundBuildOptions)" /> creates the file and stages
    /// into it, writing the container only on commit.
    /// </summary>
    [TestMethod]
    public void Create_WhenPath_ShouldCreateFileAndCommitContainer()
    {
        string path = Path.Combine(Path.GetTempPath(), $"bodu-cfb-{Guid.NewGuid():N}.cfb");

        try
        {
            using (var file = CompoundFile.Create(path))
            {
                file.RootStorage.CreateStream("Data", new byte[] { 1, 2, 3 });
                file.Commit();
            }

            using var reopened = CompoundFile.OpenRead(path);
            Assert.IsTrue(reopened.RootStorage.TryOpenStream("Data", out _));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.OpenOrCreate" /> with write access starts an empty staging tree when the
    /// destination has no existing content, rather than trying to parse zero bytes as a container.
    /// </summary>
    [TestMethod]
    public void Open_WhenOpenOrCreateOverEmptyStream_ShouldStartEmpty()
    {
        using var destination = new MemoryStream();

        using var file = CompoundFile.Open(destination, FileMode.OpenOrCreate, FileAccess.ReadWrite, leaveOpen: true);

        Assert.AreEqual(FileAccess.ReadWrite, file.Access);
        Assert.IsEmpty(file.RootStorage.EnumerateEntries());
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.OpenOrCreate" /> with read-write access over existing content loads that
    /// content for update, so a previously committed stream is still present.
    /// </summary>
    [TestMethod]
    public void Open_WhenOpenOrCreateOverExistingContent_ShouldLoadForUpdate()
    {
        using var destination = new MemoryStream();
        using (var seed = CompoundFile.Create(destination, leaveOpen: true))
        {
            seed.RootStorage.CreateStream("Original", new byte[] { 7 });
            seed.Commit();
        }

        destination.Position = 0;
        using var file = CompoundFile.Open(destination, FileMode.OpenOrCreate, FileAccess.ReadWrite, leaveOpen: true);

        Assert.IsTrue(file.RootStorage.TryOpenStream("Original", out _));
    }

    /// <summary>
    /// Verifies that <see cref="FileMode.CreateNew" /> with write access starts an empty staging tree, discarding any
    /// existing content in the destination.
    /// </summary>
    [TestMethod]
    public void Open_WhenCreateNewOverExistingContent_ShouldStartEmpty()
    {
        using var destination = new MemoryStream();
        using (var seed = CompoundFile.Create(destination, leaveOpen: true))
        {
            seed.RootStorage.CreateStream("Original", new byte[] { 7 });
            seed.Commit();
        }

        destination.Position = 0;
        using var file = CompoundFile.Open(destination, FileMode.CreateNew, FileAccess.Write, leaveOpen: true);

        Assert.IsEmpty(file.RootStorage.EnumerateEntries());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.OpenRead(string)" /> reports the <see cref="IOException" /> of a file that
    /// another handle has locked.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenFileIsLocked_ShouldThrowIOException()
    {
        string path = Path.Combine(Path.GetTempPath(), $"bodu-locked-{Guid.NewGuid():N}.cfb");
        try
        {
            File.WriteAllBytes(path, CreateContainer(("A", CompoundContainerBytes.Pattern(100))).ToArray());
            using var locker = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            Assert.ThrowsExactly<IOException>(() =>
            {
                using var file = CompoundFile.OpenRead(path);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that a buffered open reads every stream from a non-seekable source whose reads return one byte at a
    /// time.
    /// </summary>
    [TestMethod]
    public void Open_WhenBufferedSourceReturnsShortReads_ShouldReadEveryStream()
    {
        byte[] small = CompoundContainerBytes.Pattern(100);
        byte[] large = CompoundContainerBytes.Pattern(5000, 100);
        byte[] container = CreateContainer(("Small", small), ("Large", large)).ToArray();

        using var file = CompoundFile.Open(new TrickleStream(container, bytesPerRead: 1));

        using CompoundStream first = file.RootStorage.OpenStream("Small");
        CollectionAssert.AreEqual(small, first.ReadAllBytes());
        using CompoundStream second = file.RootStorage.OpenStream("Large");
        CollectionAssert.AreEqual(large, second.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that a malformed container opened with <c>leaveOpen</c> set leaves the source stream open when the open
    /// fails.
    /// </summary>
    [TestMethod]
    public void Open_WhenContainerIsMalformedAndLeaveOpenIsTrue_ShouldLeaveSourceOpen()
    {
        byte[] container = CreateContainer(("A", CompoundContainerBytes.Pattern(5000))).ToArray();
        CompoundContainerBytes.WriteUInt32(container, 0x4C, 0x00100000);
        using var source = new MemoryStream(container);

        Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundFile.Open(source, leaveOpen: true);
        });

        Assert.IsTrue(source.CanRead);
        Assert.IsTrue(source.CanSeek);
        source.Position = 0;
        Assert.AreEqual(0xD0, source.ReadByte());
    }

    /// <summary>
    /// Verifies that a buffered open reads every stream from a non-seekable source whose length is a whole number of
    /// sectors.
    /// </summary>
    [TestMethod]
    public void Open_WhenSourceIsNotSeekable_ShouldBufferEveryStream()
    {
        byte[] small = CompoundContainerBytes.Pattern(100);
        byte[] large = CompoundContainerBytes.Pattern(5000, 100);
        byte[] container = CreateContainer(("Small", small), ("Large", large)).ToArray();
        Assert.AreEqual(7680, container.Length);

        using var file = CompoundFile.Open(new Bodu.Test.IO.NonSeekableStream(container));

        using CompoundStream first = file.RootStorage.OpenStream("Small");
        CollectionAssert.AreEqual(small, first.ReadAllBytes());
        using CompoundStream second = file.RootStorage.OpenStream("Large");
        CollectionAssert.AreEqual(large, second.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that a streaming open reads every stream from a source whose reads return at most seven bytes.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamingSourceReturnsShortReads_ShouldReadEveryStream()
    {
        byte[] small = CompoundContainerBytes.Pattern(100);
        byte[] large = CompoundContainerBytes.Pattern(5000, 100);
        byte[] container = CreateContainer(("Small", small), ("Large", large)).ToArray();
        var options = new CompoundFileOptions { ReadStrategy = CompoundReadStrategy.Streaming };

        using var file = CompoundFile.Open(new Bodu.Test.IO.FixedChunkStream(container, chunkSize: 7), options);

        using CompoundStream first = file.RootStorage.OpenStream("Small");
        CollectionAssert.AreEqual(small, first.ReadAllBytes());
        using CompoundStream second = file.RootStorage.OpenStream("Large");
        CollectionAssert.AreEqual(large, second.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that a streaming open reads a version-4 stream of more than 2 GB on both sides of the 2 GB offset.
    /// </summary>
    /// <remarks>
    /// The container is a <see cref="SyntheticVersion4ContainerStream" />, whose bytes are computed as they are read, so
    /// the test neither allocates nor writes the 3 GB stream.
    /// </remarks>
    [TestMethod]
    public void Open_WhenVersion4StreamExceedsTwoGigabytes_ShouldReadAcrossTheBoundary()
    {
        const long length = 3_000_000_000;
        var options = new CompoundFileOptions { ReadStrategy = CompoundReadStrategy.Streaming };

        using var file = CompoundFile.Open(new SyntheticVersion4ContainerStream("Big", length), options);
        using CompoundStream stream = file.RootStorage.OpenStream("Big");

        Assert.AreEqual(length, stream.Length);
        foreach (long offset in new[] { 0L, 2147483647L, 2147483648L, length - 1 })
        {
            stream.Position = offset;
            Assert.AreEqual((int)CompoundContainerBytes.PatternByte(offset), stream.ReadByte(), $"offset {offset}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Strict" /> rejects a header whose fixed fields break [MS-CFB]
    /// 2.2, with <see cref="CompoundFileError.InvalidHeader" />.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="version">The version of the container patched.</param>
    /// <param name="offset">The header offset of the bytes replaced.</param>
    /// <param name="hex">The replacement bytes, in hexadecimal.</param>
    [TestMethod]
    [DataRow("HeaderClassIdNotZero", CompoundFileVersion.V3, 0x08, "0609020000000000C000000000000046")]
    [DataRow("MajorVersion5", CompoundFileVersion.V3, 0x1A, "0500")]
    [DataRow("MajorVersion4WithShift9", CompoundFileVersion.V3, 0x1A, "0400")]
    [DataRow("MajorVersion3WithShift12", CompoundFileVersion.V4, 0x1A, "0300")]
    [DataRow("ReservedNotZero", CompoundFileVersion.V3, 0x22, "000000000001")]
    [DataRow("Version3DirectorySectorsNotZero", CompoundFileVersion.V3, 0x28, "01000000")]
    [DataRow("Version4DirectorySectorsZero", CompoundFileVersion.V4, 0x28, "00000000")]
    [DataRow("Version4DirectorySectorsTooMany", CompoundFileVersion.V4, 0x28, "02000000")]
    [DataRow("MiniStreamCutoffZero", CompoundFileVersion.V3, 0x38, "00000000")]
    [DataRow("MiniStreamCutoff8192", CompoundFileVersion.V3, 0x38, "00200000")]
    public void Open_WhenHeaderBreaksAFixedFieldAtStrict_ShouldThrowInvalidHeader(string testName, CompoundFileVersion version, int offset, string hex)
    {
        byte[] container = PatchHeader(version, offset, hex);
        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundFile.Open(new MemoryStream(container), options);
        }, testName);

        Assert.AreEqual(CompoundFileError.InvalidHeader, ex.Category, testName);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Compatible" /> and <see cref="CompoundValidationLevel.Minimal" />
    /// read every stream of a container whose header breaks a fixed field of [MS-CFB] 2.2, taking the mini stream cutoff
    /// as 4096 whatever the header declares.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="version">The version of the container patched.</param>
    /// <param name="offset">The header offset of the bytes replaced.</param>
    /// <param name="hex">The replacement bytes, in hexadecimal.</param>
    [TestMethod]
    [DataRow("HeaderClassIdNotZero", CompoundFileVersion.V3, 0x08, "0609020000000000C000000000000046")]
    [DataRow("MajorVersion5", CompoundFileVersion.V3, 0x1A, "0500")]
    [DataRow("MajorVersion4WithShift9", CompoundFileVersion.V3, 0x1A, "0400")]
    [DataRow("MajorVersion3WithShift12", CompoundFileVersion.V4, 0x1A, "0300")]
    [DataRow("ReservedNotZero", CompoundFileVersion.V3, 0x22, "000000000001")]
    [DataRow("Version3DirectorySectorsNotZero", CompoundFileVersion.V3, 0x28, "01000000")]
    [DataRow("Version4DirectorySectorsZero", CompoundFileVersion.V4, 0x28, "00000000")]
    [DataRow("Version4DirectorySectorsTooMany", CompoundFileVersion.V4, 0x28, "02000000")]
    [DataRow("MiniStreamCutoffZero", CompoundFileVersion.V3, 0x38, "00000000")]
    [DataRow("MiniStreamCutoff8192", CompoundFileVersion.V3, 0x38, "00200000")]
    public void Open_WhenHeaderBreaksAFixedFieldAtTolerantLevels_ShouldReadEveryStream(string testName, CompoundFileVersion version, int offset, string hex)
    {
        byte[] container = PatchHeader(version, offset, hex);

        foreach (CompoundValidationLevel level in new[] { CompoundValidationLevel.Compatible, CompoundValidationLevel.Minimal })
        {
            var options = new CompoundFileOptions { ValidationLevel = level };
            using var file = CompoundFile.Open(new MemoryStream(container), options);
            using CompoundStream small = file.RootStorage.OpenStream("Small");
            using CompoundStream large = file.RootStorage.OpenStream("Large");

            CollectionAssert.AreEqual(CompoundContainerBytes.Pattern(100), small.ReadAllBytes(), $"{testName} at {level}");
            CollectionAssert.AreEqual(CompoundContainerBytes.Pattern(5000, 100), large.ReadAllBytes(), $"{testName} at {level}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Strict" /> reads a container whose header is conformant,
    /// including a version-4 header that counts a directory of several sectors.
    /// </summary>
    /// <param name="version">The version of the container.</param>
    /// <param name="streamCount">The number of streams in the root storage.</param>
    [TestMethod]
    [DataRow(CompoundFileVersion.V3, 2)]
    [DataRow(CompoundFileVersion.V4, 2)]
    [DataRow(CompoundFileVersion.V4, 40)]
    public void Open_WhenHeaderIsConformantAtStrict_ShouldReadEveryStream(CompoundFileVersion version, int streamCount)
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        for (int i = 0; i < streamCount; i++)
            _ = builder.AddStream($"Stream{i}", CompoundContainerBytes.Pattern(100 * (i + 1)));

        byte[] container = builder.ToArray(new CompoundBuildOptions { Version = version });
        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };

        using var file = CompoundFile.Open(new MemoryStream(container), options);

        Assert.HasCount(streamCount, file.RootStorage.EnumerateStreams());
    }

    /// <summary>
    /// Builds a container holding a 100-byte stream in the mini stream and a 5000-byte stream in regular sectors, and
    /// replaces bytes of its header.
    /// </summary>
    /// <param name="version">The version of the container.</param>
    /// <param name="offset">The header offset of the bytes replaced.</param>
    /// <param name="hex">The replacement bytes, in hexadecimal.</param>
    /// <returns>The patched container.</returns>
    private static byte[] PatchHeader(CompoundFileVersion version, int offset, string hex)
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("Small", CompoundContainerBytes.Pattern(100));
        _ = builder.AddStream("Large", CompoundContainerBytes.Pattern(5000, 100));
        byte[] container = builder.ToArray(new CompoundBuildOptions { Version = version });

        Convert.FromHexString(hex).CopyTo(container, offset);
        return container;
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Strict" /> rejects allocation tables that break [MS-CFB] even
    /// where no chain the reader follows needs the broken entry, with the category the issue names for each defect.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the defect.</param>
    /// <param name="category">The category the rejection reports.</param>
    [TestMethod]
    [DataRow("FatSectorNotMarkedFatSect", CompoundFileError.InvalidDifat)]
    [DataRow("DifatSectorNotMarkedDifSect", CompoundFileError.InvalidDifat)]
    [DataRow("DifatChainEndsInFreeSect", CompoundFileError.InvalidDifat)]
    [DataRow("DifatChainEndsInDifSect", CompoundFileError.InvalidDifat)]
    [DataRow("FatEntryPastEndOfFileNotFree", CompoundFileError.SectorOutOfRange)]
    [DataRow("FatEntryPastEndOfFileMarkedFatSect", CompoundFileError.SectorOutOfRange)]
    [DataRow("RegularSectorInTwoChains", CompoundFileError.FatCycle)]
    [DataRow("MiniSectorInTwoChains", CompoundFileError.InvalidMiniFat)]
    public void Open_WhenAllocationTablesBreakMsCfbAtStrict_ShouldThrow(string testName, CompoundFileError category)
    {
        byte[] container = BuildAllocationCase(testName, out _);
        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundFile.Open(new MemoryStream(container), options);
        }, testName);

        Assert.AreEqual(category, ex.Category, testName);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Compatible" /> and <see cref="CompoundValidationLevel.Minimal" />
    /// open containers whose allocation tables break [MS-CFB] only where no chain they follow needs the broken entry,
    /// including a DIFAT chain that ends in DIFSECT, and read the first stream.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the defect.</param>
    [TestMethod]
    [DataRow("FatSectorNotMarkedFatSect")]
    [DataRow("DifatSectorNotMarkedDifSect")]
    [DataRow("DifatChainEndsInFreeSect")]
    [DataRow("DifatChainEndsInDifSect")]
    [DataRow("FatEntryPastEndOfFileNotFree")]
    [DataRow("FatEntryPastEndOfFileMarkedFatSect")]
    [DataRow("RegularSectorInTwoChains")]
    [DataRow("MiniSectorInTwoChains")]
    public void Open_WhenAllocationTablesBreakMsCfbAtTolerantLevels_ShouldReadTheFirstStream(string testName)
    {
        byte[] container = BuildAllocationCase(testName, out byte[] first);

        foreach (CompoundValidationLevel level in new[] { CompoundValidationLevel.Compatible, CompoundValidationLevel.Minimal })
        {
            var options = new CompoundFileOptions { ValidationLevel = level };
            using var file = CompoundFile.Open(new MemoryStream(container), options);
            using CompoundStream stream = file.RootStorage.OpenStream("A");

            CollectionAssert.AreEqual(first, stream.ReadAllBytes(), $"{testName} at {level}");
        }
    }

    /// <summary>
    /// Builds a container whose allocation tables break [MS-CFB] in the way a case names.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="first">Receives the content of the container's first stream, <c>A</c>.</param>
    /// <returns>The container.</returns>
    private static byte[] BuildAllocationCase(string testName, out byte[] first)
    {
        // A 7 MiB stream needs 113 FAT sectors, so the writer lists four of them in a DIFAT sector.
        bool needsDifat = testName.StartsWith("Difat", StringComparison.Ordinal);
        int firstLength = needsDifat ? 7340032 : testName.StartsWith("Mini", StringComparison.Ordinal) ? 100 : 5000;
        first = CompoundContainerBytes.Pattern(firstLength);
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", first);
        _ = builder.AddStream("B", CompoundContainerBytes.Pattern(firstLength == 100 ? 100 : 5000, 7));
        byte[] container = builder.ToArray();

        var header = CfbHeader.Parse(container);
        uint sectorsInFile = (uint)((container.Length / header.SectorSize) - 1);
        switch (testName)
        {
            case "FatSectorNotMarkedFatSect":
                CompoundContainerBytes.SetFatEntry(container, CompoundContainerBytes.FatSectorIds(container)[0], CfbHeader.EndOfChain);
                break;

            case "DifatSectorNotMarkedDifSect":
                CompoundContainerBytes.SetFatEntry(container, CompoundContainerBytes.DifatSectorIds(container)[0], CfbHeader.EndOfChain);
                break;

            case "DifatChainEndsInFreeSect":
            case "DifatChainEndsInDifSect":
                uint last = CompoundContainerBytes.DifatSectorIds(container)[^1];
                int next = (int)(CompoundContainerBytes.SectorOffset(header, last) + header.SectorSize - 4);
                CompoundContainerBytes.WriteUInt32(container, next, testName.EndsWith("FreeSect", StringComparison.Ordinal) ? CfbHeader.FreeSector : CfbHeader.DifatSector);
                break;

            case "FatEntryPastEndOfFileNotFree":
                CompoundContainerBytes.SetFatEntry(container, sectorsInFile, 0x1E555E69);
                break;

            case "FatEntryPastEndOfFileMarkedFatSect":
                CompoundContainerBytes.SetFatEntry(container, sectorsInFile, CfbHeader.FatSector);
                break;

            default:
                int a = CompoundContainerBytes.FindEntryOffset(container, "A");
                int b = CompoundContainerBytes.FindEntryOffset(container, "B");
                container.AsSpan(a + CompoundContainerBytes.StartSectorOffset, 4).CopyTo(container.AsSpan(b + CompoundContainerBytes.StartSectorOffset));
                break;
        }

        return container;
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Strict" /> rejects a directory entry that breaks [MS-CFB] 2.6
    /// with <see cref="CompoundFileError.InvalidDirectory" />.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the defect.</param>
    [TestMethod]
    [DataRow("RootNotNamedRootEntry")]
    [DataRow("StreamClassIdNotZero")]
    [DataRow("StreamCreationTimeNotZero")]
    [DataRow("StreamModifiedTimeNotZero")]
    [DataRow("RootCreationTimeNotZero")]
    [DataRow("StorageStartSectorNotZero")]
    [DataRow("NameLengthOdd")]
    [DataRow("NameWithoutTerminator")]
    [DataRow("RedNodeWithRedSibling")]
    public void Open_WhenDirectoryEntryBreaksMsCfbAtStrict_ShouldThrowInvalidDirectory(string testName)
    {
        byte[] container = BuildDirectoryCase(testName);
        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundFile.Open(new MemoryStream(container), options);
        }, testName);

        Assert.AreEqual(CompoundFileError.InvalidDirectory, ex.Category, testName);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Compatible" /> and <see cref="CompoundValidationLevel.Minimal" />
    /// read a directory entry whose fields break [MS-CFB] 2.6 without making its name unreadable, a name without its
    /// terminator included.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the defect.</param>
    [TestMethod]
    [DataRow("RootNotNamedRootEntry")]
    [DataRow("StreamClassIdNotZero")]
    [DataRow("StreamCreationTimeNotZero")]
    [DataRow("StreamModifiedTimeNotZero")]
    [DataRow("RootCreationTimeNotZero")]
    [DataRow("StorageStartSectorNotZero")]
    [DataRow("NameWithoutTerminator")]
    [DataRow("RedNodeWithRedSibling")]
    public void Open_WhenDirectoryEntryBreaksMsCfbAtTolerantLevels_ShouldReadTheEntry(string testName)
    {
        byte[] container = BuildDirectoryCase(testName);

        foreach (CompoundValidationLevel level in new[] { CompoundValidationLevel.Compatible, CompoundValidationLevel.Minimal })
        {
            var options = new CompoundFileOptions { ValidationLevel = level };
            using var file = CompoundFile.Open(new MemoryStream(container), options);
            using CompoundStream stream = file.RootStorage.OpenStream("A");

            CollectionAssert.AreEqual(CompoundContainerBytes.Pattern(100), stream.ReadAllBytes(), $"{testName} at {level}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Minimal" /> skips a directory entry whose name length is odd,
    /// as it skips an entry of unknown type, instead of reading a name cut short.
    /// </summary>
    /// <param name="nameLength">The odd name length written over the entry's own length of 8.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(7)]
    public void Open_WhenNameLengthIsOddAtMinimal_ShouldSkipTheEntry(int nameLength)
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("ABC", CompoundContainerBytes.Pattern(100));
        _ = builder.AddStream("D", CompoundContainerBytes.Pattern(100));
        byte[] container = builder.ToArray();
        int entry = CompoundContainerBytes.FindEntryOffset(container, "ABC");
        BinaryPrimitives.WriteUInt16LittleEndian(container.AsSpan(entry + CompoundContainerBytes.NameLengthOffset), (ushort)nameLength);
        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Minimal };

        using var file = CompoundFile.Open(new MemoryStream(container), options);

        CollectionAssert.AreEqual(new[] { "D" }, file.RootStorage.EnumerateEntries().Select(static e => e.Name).ToArray());
    }

    /// <summary>
    /// Builds a container whose directory breaks [MS-CFB] 2.6 in the way a case names: its root holds a 100-byte stream
    /// <c>A</c>, a 100-byte stream <c>B</c> and a storage <c>S</c> holding a 100-byte stream <c>C</c>.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <returns>The container.</returns>
    private static byte[] BuildDirectoryCase(string testName)
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", CompoundContainerBytes.Pattern(100));
        _ = builder.AddStream("B", CompoundContainerBytes.Pattern(100, 7));
        _ = builder.AddStorage("S").AddStream("C", CompoundContainerBytes.Pattern(100, 9));
        byte[] container = builder.ToArray();

        int root = CompoundContainerBytes.FindEntryOffset(container, "Root Entry");
        int a = CompoundContainerBytes.FindEntryOffset(container, "A");
        switch (testName)
        {
            case "RootNotNamedRootEntry":
                container.AsSpan(root, 64).Clear();
                System.Text.Encoding.Unicode.GetBytes("foo").CopyTo(container, root);
                BinaryPrimitives.WriteUInt16LittleEndian(container.AsSpan(root + CompoundContainerBytes.NameLengthOffset), 8);
                break;

            case "StreamClassIdNotZero":
                new Guid("00020906-0000-0000-C000-000000000046").TryWriteBytes(container.AsSpan(a + CompoundContainerBytes.ClassIdOffset));
                break;

            case "StreamCreationTimeNotZero":
                CompoundContainerBytes.WriteInt64(container, a + CompoundContainerBytes.CreationTimeOffset, 0x01D0000000000000);
                break;

            case "StreamModifiedTimeNotZero":
                CompoundContainerBytes.WriteInt64(container, a + CompoundContainerBytes.ModifiedTimeOffset, 0x01D0000000000000);
                break;

            case "RootCreationTimeNotZero":
                CompoundContainerBytes.WriteInt64(container, root + CompoundContainerBytes.CreationTimeOffset, 0x01D0000000000000);
                break;

            case "StorageStartSectorNotZero":
                CompoundContainerBytes.WriteUInt32(container, CompoundContainerBytes.FindEntryOffset(container, "S") + CompoundContainerBytes.StartSectorOffset, 7);
                break;

            case "NameLengthOdd":
                BinaryPrimitives.WriteUInt16LittleEndian(container.AsSpan(a + CompoundContainerBytes.NameLengthOffset), 5);
                break;

            case "NameWithoutTerminator":
                BinaryPrimitives.WriteUInt16LittleEndian(container.AsSpan(a + 2), 0x42);
                break;

            default:
                // The root's children sort A, B, S, so the writer roots their tree at B with A and S as its siblings.
                int b = CompoundContainerBytes.FindEntryOffset(container, "B");
                container[a + CompoundContainerBytes.ColorOffset] = (byte)CompoundEntryColor.Red;
                container[b + CompoundContainerBytes.ColorOffset] = (byte)CompoundEntryColor.Red;
                break;
        }

        return container;
    }
}
