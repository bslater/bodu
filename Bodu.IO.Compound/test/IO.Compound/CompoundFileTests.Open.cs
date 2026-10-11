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

    /// <summary>
    /// Verifies that every level and read strategy reads a container whose final sector the file cuts short after the
    /// last byte any structure declares, as [MS-CFB] 2.3 allows.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the structure in the final sector.</param>
    [TestMethod]
    [DataRow("RegularStream")]
    [DataRow("MiniStream")]
    [DataRow("DirectoryCutInAFreeEntry")]
    [DataRow("Version4RegularStream")]
    public void Open_WhenFinalSectorIsShortButHoldsEveryDeclaredByte_ShouldReadEveryStream(string testName)
    {
        byte[] container = BuildShortSectorCase(testName, missingBytes: 0, out byte[] a, out byte[] b);

        foreach (CompoundValidationLevel level in Enum.GetValues<CompoundValidationLevel>())
        {
            foreach (CompoundReadStrategy strategy in new[] { CompoundReadStrategy.Buffered, CompoundReadStrategy.Streaming })
            {
                var options = new CompoundFileOptions { ValidationLevel = level, ReadStrategy = strategy };
                using var file = CompoundFile.Open(new MemoryStream(container), options);
                using CompoundStream first = file.RootStorage.OpenStream("A");
                using CompoundStream second = file.RootStorage.OpenStream("B");

                CollectionAssert.AreEqual(a, first.ReadAllBytes(), $"{testName} at {level} {strategy}");
                CollectionAssert.AreEqual(b, second.ReadAllBytes(), $"{testName} at {level} {strategy}");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Strict" /> and <see cref="CompoundValidationLevel.Compatible" />
    /// report <see cref="CompoundFileError.TruncatedFile" /> when the file cuts its final sector short before a byte a
    /// structure declares, through either read strategy.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the structure in the final sector.</param>
    [TestMethod]
    [DataRow("RegularStream")]
    [DataRow("MiniStream")]
    [DataRow("DirectoryCutInAReachedEntry")]
    [DataRow("FatSector")]
    [DataRow("Version4RegularStream")]
    public void Open_WhenFinalSectorIsShortOfADeclaredByte_ShouldThrowTruncatedFile(string testName)
    {
        byte[] container = BuildShortSectorCase(testName, missingBytes: 1, out _, out _);

        foreach (CompoundValidationLevel level in new[] { CompoundValidationLevel.Strict, CompoundValidationLevel.Compatible })
        {
            foreach (CompoundReadStrategy strategy in new[] { CompoundReadStrategy.Buffered, CompoundReadStrategy.Streaming })
            {
                var options = new CompoundFileOptions { ValidationLevel = level, ReadStrategy = strategy };

                CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
                {
                    using var file = CompoundFile.Open(new MemoryStream(container), options);
                    using CompoundStream first = file.RootStorage.OpenStream("A");
                    using CompoundStream second = file.RootStorage.OpenStream("B");
                    _ = first.ReadAllBytes();
                    _ = second.ReadAllBytes();
                }, $"{testName} at {level} {strategy}");

                Assert.AreEqual(CompoundFileError.TruncatedFile, ex.Category, $"{testName} at {level} {strategy}");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Minimal" /> reads a declared byte that the file cuts from its
    /// final sector as zero, through either read strategy.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the structure in the final sector.</param>
    [TestMethod]
    [DataRow("RegularStream")]
    [DataRow("MiniStream")]
    [DataRow("FatSector")]
    [DataRow("Version4RegularStream")]
    public void Open_WhenFinalSectorIsShortOfADeclaredByteAtMinimal_ShouldReadItAsZero(string testName)
    {
        byte[] container = BuildShortSectorCase(testName, missingBytes: 1, out byte[] a, out byte[] b);

        // The cut removes the last byte of A's data, or of B's in the mini stream; a FAT sector's missing entries read
        // as free, and no chain needs them.
        if (testName == "MiniStream")
            b[^1] = 0;
        else if (testName != "FatSector")
            a[^1] = 0;

        foreach (CompoundReadStrategy strategy in new[] { CompoundReadStrategy.Buffered, CompoundReadStrategy.Streaming })
        {
            var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Minimal, ReadStrategy = strategy };
            using var file = CompoundFile.Open(new MemoryStream(container), options);
            using CompoundStream first = file.RootStorage.OpenStream("A");
            using CompoundStream second = file.RootStorage.OpenStream("B");

            CollectionAssert.AreEqual(a, first.ReadAllBytes(), $"{testName} {strategy}");
            CollectionAssert.AreEqual(b, second.ReadAllBytes(), $"{testName} {strategy}");
        }
    }

    /// <summary>
    /// Builds a container whose final sector the file cuts short: the last sector of a structure is moved to the end of
    /// the file and cut after the bytes the structure declares, less the bytes a case leaves missing.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="missingBytes">The number of declared bytes the cut removes.</param>
    /// <param name="a">Receives the content of stream <c>A</c>, the stream the cut reaches in the stream cases.</param>
    /// <param name="b">Receives the content of stream <c>B</c>.</param>
    /// <returns>The container.</returns>
    private static byte[] BuildShortSectorCase(string testName, int missingBytes, out byte[] a, out byte[] b)
    {
        bool mini = testName == "MiniStream" || testName.StartsWith("Directory", StringComparison.Ordinal);
        a = CompoundContainerBytes.Pattern(mini ? 100 : 5000);
        b = CompoundContainerBytes.Pattern(testName == "MiniStream" ? 128 : 100, 5000);
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", a);
        _ = builder.AddStream("B", b);
        CompoundFileVersion version = testName.StartsWith("Version4", StringComparison.Ordinal) ? CompoundFileVersion.V4 : CompoundFileVersion.V3;
        byte[] container = builder.ToArray(new CompoundBuildOptions { Version = version });
        var header = CfbHeader.Parse(container);

        // The writer lays out the directory, the mini FAT, the mini stream and the regular streams, and then the FAT.
        switch (testName)
        {
            case "RegularStream":
            case "Version4RegularStream":
                int entry = CompoundContainerBytes.FindEntryOffset(container, "A");
                byte[] moved = CompoundContainerBytes.MoveLastSector(container, entry + CompoundContainerBytes.StartSectorOffset);
                return moved[..(container.Length + (a.Length % header.SectorSize) - missingBytes)];

            case "MiniStream":
                // A's 100 bytes take two mini sectors and B's 128 the next two, so the mini stream declares 256 bytes,
                // the last of them B's.
                int root = CompoundContainerBytes.FindEntryOffset(container, "Root Entry");
                byte[] movedMini = CompoundContainerBytes.MoveLastSector(container, root + CompoundContainerBytes.StartSectorOffset);
                return movedMini[..(container.Length + 256 - missingBytes)];

            case "DirectoryCutInAFreeEntry":
            case "DirectoryCutInAReachedEntry":
                // The directory holds the root, A and B, then a free entry; the cut falls in the free entry, or in B.
                byte[] movedDirectory = CompoundContainerBytes.MoveLastSector(container, 0x30);
                return movedDirectory[..(container.Length + (testName.EndsWith("FreeEntry", StringComparison.Ordinal) ? 444 : 300))];

            default:
                // The FAT is the last sector the writer emits; every byte of it is declared.
                return container[..^missingBytes];
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Strict" /> rejects a header whose Number of FAT Sectors or
    /// Number of DIFAT Sectors disagrees with the tables, with <see cref="CompoundFileError.InvalidDifat" />.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the count and its value.</param>
    [TestMethod]
    [DataRow("FatSectorCountTooHigh")]
    [DataRow("FatSectorCountTooLow")]
    [DataRow("DifatSectorCountZeroWithAChain")]
    [DataRow("DifatSectorCountTooHigh")]
    [DataRow("DifatSectorCountWithoutAChain")]
    public void Open_WhenHeaderCountsDisagreeWithTheTablesAtStrict_ShouldThrowInvalidDifat(string testName)
    {
        byte[] container = BuildCountCase(testName, out _);
        var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundFile.Open(new MemoryStream(container), options);
        }, testName);

        Assert.AreEqual(CompoundFileError.InvalidDifat, ex.Category, testName);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Compatible" /> and <see cref="CompoundValidationLevel.Minimal" />
    /// ignore the header's Number of FAT Sectors and Number of DIFAT Sectors and follow the DIFAT, except that a count of
    /// zero DIFAT sectors means there is no DIFAT chain, whatever the First DIFAT Sector Location holds.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the count and its value.</param>
    [TestMethod]
    [DataRow("FatSectorCountTooHigh")]
    [DataRow("FatSectorCountTooLow")]
    [DataRow("DifatSectorCountTooHigh")]
    [DataRow("DifatSectorCountWithoutAChain")]
    [DataRow("DifatSectorCountZeroWithFirstDifatSectorPastTheEnd")]
    public void Open_WhenHeaderCountsDisagreeWithTheTablesAtTolerantLevels_ShouldReadTheFirstStream(string testName)
    {
        byte[] container = BuildCountCase(testName, out byte[] first);

        foreach (CompoundValidationLevel level in new[] { CompoundValidationLevel.Compatible, CompoundValidationLevel.Minimal })
        {
            var options = new CompoundFileOptions { ValidationLevel = level };
            using var file = CompoundFile.Open(new MemoryStream(container), options);
            using CompoundStream stream = file.RootStorage.OpenStream("A");

            CollectionAssert.AreEqual(first, stream.ReadAllBytes(), $"{testName} at {level}");
        }
    }

    /// <summary>
    /// Builds a container whose header counts disagree with its tables in the way a case names.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="first">Receives the content of the container's stream <c>A</c>.</param>
    /// <returns>The container.</returns>
    private static byte[] BuildCountCase(string testName, out byte[] first)
    {
        // A 7 MiB stream needs 113 FAT sectors, four of them listed in the one DIFAT sector.
        bool needsDifat = testName is "DifatSectorCountZeroWithAChain" or "DifatSectorCountTooHigh";
        first = CompoundContainerBytes.Pattern(needsDifat ? 7340032 : 100);
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", first);
        byte[] container = builder.ToArray();

        switch (testName)
        {
            case "FatSectorCountTooHigh":
                CompoundContainerBytes.WriteUInt32(container, 0x2C, 0x20202001);
                break;

            case "FatSectorCountTooLow":
                CompoundContainerBytes.WriteUInt32(container, 0x2C, 0);
                break;

            case "DifatSectorCountZeroWithAChain":
                CompoundContainerBytes.WriteUInt32(container, 0x48, 0);
                break;

            case "DifatSectorCountTooHigh":
                CompoundContainerBytes.WriteUInt32(container, 0x48, 2);
                break;

            case "DifatSectorCountWithoutAChain":
                CompoundContainerBytes.WriteUInt32(container, 0x48, 1);
                break;

            default:
                CompoundContainerBytes.WriteUInt32(container, 0x44, 0x00100000);
                break;
        }

        return container;
    }

    /// <summary>
    /// Verifies that a malformed container is rejected under the <see cref="CompoundFileError" /> category that
    /// documents its defect, at <see cref="CompoundValidationLevel.Strict" /> and
    /// <see cref="CompoundValidationLevel.Compatible" /> and through either read strategy.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the defect.</param>
    /// <param name="category">The category that documents the defect.</param>
    [TestMethod]
    [DataRow("MiniStreamStartsPastTheMiniFat", CompoundFileError.InvalidMiniFat)]
    [DataRow("MiniChainLeavesTheMiniFat", CompoundFileError.InvalidMiniFat)]
    [DataRow("FirstMiniFatSectorIsFree", CompoundFileError.InvalidMiniFat)]
    [DataRow("Version4ShorterThanItsHeaderSector", CompoundFileError.TruncatedFile)]
    public void Open_WhenContainerIsMalformed_ShouldReportTheDocumentedCategory(string testName, CompoundFileError category)
    {
        byte[] container = BuildCategoryCase(testName);

        foreach (CompoundValidationLevel level in new[] { CompoundValidationLevel.Strict, CompoundValidationLevel.Compatible })
        {
            foreach (CompoundReadStrategy strategy in new[] { CompoundReadStrategy.Buffered, CompoundReadStrategy.Streaming })
            {
                var options = new CompoundFileOptions { ValidationLevel = level, ReadStrategy = strategy };

                CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
                {
                    using var file = CompoundFile.Open(new MemoryStream(container), options);
                    using CompoundStream stream = file.RootStorage.OpenStream("A");
                    _ = stream.ReadAllBytes();
                }, $"{testName} at {level} {strategy}");

                Assert.AreEqual(category, ex.Category, $"{testName} at {level} {strategy}");
            }
        }
    }

    /// <summary>
    /// Builds a container malformed in the way a case names; its root holds an 80-byte stream <c>A</c>, the first in the
    /// mini stream.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <returns>The container.</returns>
    private static byte[] BuildCategoryCase(string testName)
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", CompoundContainerBytes.Pattern(80));
        CompoundFileVersion version = testName.StartsWith("Version4", StringComparison.Ordinal) ? CompoundFileVersion.V4 : CompoundFileVersion.V3;
        byte[] container = builder.ToArray(new CompoundBuildOptions { Version = version });
        var header = CfbHeader.Parse(container);

        switch (testName)
        {
            case "MiniStreamStartsPastTheMiniFat":
                CompoundContainerBytes.WriteUInt32(container, CompoundContainerBytes.FindEntryOffset(container, "A") + CompoundContainerBytes.StartSectorOffset, 123456789);
                return container;

            case "MiniChainLeavesTheMiniFat":
                // A's first mini sector is mini sector 0, whose mini FAT entry names a mini sector past the mini FAT.
                CompoundContainerBytes.WriteUInt32(container, (int)CompoundContainerBytes.SectorOffset(header, header.FirstMiniFatSector), 0x00FFFFFF);
                return container;

            case "FirstMiniFatSectorIsFree":
                CompoundContainerBytes.WriteUInt32(container, 0x3C, CfbHeader.FreeSector);
                return container;

            default:
                return container[..1000];
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Compatible" /> prunes a sibling link to a directory entry it
    /// skipped as malformed, and reads the other entries, as <see cref="CompoundValidationLevel.Minimal" /> does.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the malformed entry.</param>
    [TestMethod]
    [DataRow("UnknownType")]
    [DataRow("NameLengthAbove64")]
    [DataRow("NameLengthOdd")]
    [DataRow("WorkbookWithMalformedThumbnail")]
    public void Open_WhenSiblingLinkReachesASkippedEntry_ShouldPruneTheLink(string testName)
    {
        byte[] container = BuildSkippedEntryCase(testName, out string[] kept);

        foreach (CompoundValidationLevel level in new[] { CompoundValidationLevel.Compatible, CompoundValidationLevel.Minimal })
        {
            var options = new CompoundFileOptions { ValidationLevel = level };
            using var file = CompoundFile.Open(new MemoryStream(container), options);

            CollectionAssert.AreEqual(kept, file.RootStorage.EnumerateEntries().Select(static e => e.Name).ToArray(), $"{testName} at {level}");
            foreach (string name in kept)
            {
                using CompoundStream stream = file.RootStorage.OpenStream(name);
                Assert.AreEqual(10, stream.ReadAllBytes().Length, $"{testName} at {level}");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="CompoundValidationLevel.Compatible" /> still rejects, as
    /// <see cref="CompoundFileError.DirectoryCycle" />, a sibling link that leaves the directory, reaches a free slot or
    /// reaches an entry already in the tree.
    /// </summary>
    /// <param name="testName">The name of the case, which selects the link.</param>
    [TestMethod]
    [DataRow("LinkOutOfRange")]
    [DataRow("LinkToAFreeSlot")]
    [DataRow("LinkToAnEntryInTheTree")]
    public void Open_WhenSiblingLinkIsInvalidAtCompatible_ShouldThrowDirectoryCycle(string testName)
    {
        byte[] container = BuildSkippedEntryCase(testName, out _);

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundFile.Open(new MemoryStream(container));
        }, testName);

        Assert.AreEqual(CompoundFileError.DirectoryCycle, ex.Category, testName);
    }

    /// <summary>
    /// Builds a container whose root holds the 10-byte streams <c>A</c>, <c>B</c>, <c>C</c> and <c>D</c>, or a
    /// workbook's <c>Workbook</c> and <c>Thumbnail</c>, and breaks one entry or link in the way a case names.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="kept">Receives the names a level that prunes the broken link lists, in directory order.</param>
    /// <returns>The container.</returns>
    private static byte[] BuildSkippedEntryCase(string testName, out string[] kept)
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        bool workbook = testName == "WorkbookWithMalformedThumbnail";
        string[] names = workbook ? ["Workbook", "Thumbnail"] : ["A", "B", "C", "D"];
        foreach (string name in names)
            _ = builder.AddStream(name, CompoundContainerBytes.Pattern(10));

        byte[] container = builder.ToArray();

        // The writer roots A..D at B, with A to its left and C to its right, and D to the right of C; it roots the
        // workbook's two streams at Workbook, the shorter name, with Thumbnail to its right.
        int c = CompoundContainerBytes.FindEntryOffset(container, workbook ? "Thumbnail" : "C");
        int b = workbook ? 0 : CompoundContainerBytes.FindEntryOffset(container, "B");
        kept = workbook ? ["Workbook"] : ["A", "B"];
        switch (testName)
        {
            case "UnknownType":
                container[c + CompoundContainerBytes.TypeOffset] = 3;
                break;

            case "NameLengthAbove64":
            case "WorkbookWithMalformedThumbnail":
                BinaryPrimitives.WriteUInt16LittleEndian(container.AsSpan(c + CompoundContainerBytes.NameLengthOffset), 200);
                break;

            case "NameLengthOdd":
                BinaryPrimitives.WriteUInt16LittleEndian(container.AsSpan(c + CompoundContainerBytes.NameLengthOffset), 3);
                break;

            case "LinkOutOfRange":
                CompoundContainerBytes.WriteUInt32(container, b + CompoundContainerBytes.RightSiblingOffset, 1000);
                break;

            case "LinkToAFreeSlot":
                CompoundContainerBytes.WriteUInt32(container, b + CompoundContainerBytes.RightSiblingOffset, 6);
                break;

            default:
                CompoundContainerBytes.WriteUInt32(container, b + CompoundContainerBytes.RightSiblingOffset, CompoundContainerBytes.FindStreamId(container, "A"));
                break;
        }

        return container;
    }
}
