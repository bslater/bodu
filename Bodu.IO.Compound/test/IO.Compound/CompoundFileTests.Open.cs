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
            Assert.IsTrue(file.RootStorage.EnumerateEntries().Any());
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

            using CompoundFile file = CompoundFile.OpenRead(path);

            Assert.AreEqual(FileAccess.Read, file.Access);
            Assert.IsTrue(file.RootStorage.EnumerateEntries().Any());
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

            using CompoundFile file = CompoundFile.Open(path, FileMode.Open, FileAccess.Read);

            Assert.AreEqual(FileAccess.Read, file.Access);
            Assert.IsTrue(file.RootStorage.EnumerateEntries().Any());
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
            using (CompoundFile file = CompoundFile.Open(path, FileMode.Create, FileAccess.ReadWrite))
            {
                using (CompoundStream stream = file.RootStorage.CreateStream("Data"))
                    stream.Write(payload, 0, payload.Length);

                file.Commit();
            }

            using CompoundFile reopened = CompoundFile.Open(path, FileMode.Open, FileAccess.Read);
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
            using (CompoundFile file = CompoundFile.Open(path, FileMode.Create, FileAccess.ReadWrite))
            {
                file.RootStorage.CreateStream("Original", new byte[] { 9 });
                file.Commit();
            }

            using (CompoundFile file = CompoundFile.Open(path, FileMode.Open, FileAccess.ReadWrite))
            {
                file.RootStorage.CreateStream("Added", new byte[] { 8 });
                file.Commit();
            }

            using CompoundFile reopened = CompoundFile.Open(path, FileMode.Open, FileAccess.Read);
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
}
