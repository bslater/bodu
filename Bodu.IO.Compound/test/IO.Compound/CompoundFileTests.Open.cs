// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
