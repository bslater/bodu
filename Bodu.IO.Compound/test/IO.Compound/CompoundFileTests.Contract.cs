// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Contract.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundFile.Revert" /> in update mode restores the streams loaded at open time,
    /// discarding additions and deletions.
    /// </summary>
    [TestMethod]
    public void Revert_WhenOpenedForUpdate_ShouldRestoreOriginalStreams()
    {
        MemoryStream destination = CreateContainer(("A", [1]), ("B", [2]));
        destination.Position = 0;

        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        file.RootStorage.CreateStream("C", new byte[] { 3 });
        Assert.IsTrue(file.RootStorage.Delete("A"));

        file.Revert();

        Assert.IsFalse(file.IsDirty);
        Assert.IsTrue(file.RootStorage.TryOpenStream("A", out _));
        Assert.IsTrue(file.RootStorage.TryOpenStream("B", out _));
        Assert.IsFalse(file.RootStorage.TryOpenStream("C", out _));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.Revert" /> restores an overwritten stream's original content.
    /// </summary>
    [TestMethod]
    public void Revert_WhenOpenedForUpdateAfterOverwrite_ShouldRestoreOriginalContent()
    {
        MemoryStream destination = CreateContainer(("Data", [1, 2, 3]));
        destination.Position = 0;

        using var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        using (CompoundStream overwrite = file.RootStorage.OpenStream("Data", FileMode.Create, FileAccess.Write))
            overwrite.Write([9, 9], 0, 2);

        file.Revert();

        using CompoundStream restored = file.RootStorage.OpenStream("Data");
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, restored.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.Revert" /> in update mode can be committed afterwards to reproduce the
    /// original file.
    /// </summary>
    [TestMethod]
    public void Revert_WhenOpenedForUpdate_ShouldCommitOriginalAfterRevert()
    {
        MemoryStream destination = CreateContainer(("Keep", [7]));
        destination.Position = 0;

        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            file.RootStorage.CreateStream("Extra", new byte[] { 8 });
            file.Revert();
            file.Commit();
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);
        Assert.IsTrue(reopened.RootStorage.TryOpenStream("Keep", out _));
        Assert.IsFalse(reopened.RootStorage.TryOpenStream("Extra", out _));
    }

    /// <summary>
    /// Verifies that the mode/access <see cref="CompoundStorage.TryOpenStream(string, FileMode, FileAccess, out CompoundStream)" />
    /// returns <see langword="false" /> for a missing stream opened with <see cref="FileMode.Open" /> instead of
    /// throwing.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenWritableAndMissingWithModeOpen_ShouldReturnFalse()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);

        Assert.IsFalse(file.RootStorage.TryOpenStream("Nope", FileMode.Open, FileAccess.ReadWrite, out CompoundStream? stream));
        Assert.IsNull(stream);
    }

    /// <summary>
    /// Verifies that the mode/access <c>TryOpenStream</c> returns <see langword="false" /> for
    /// <see cref="FileMode.CreateNew" /> when the stream already exists.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenCreateNewAndExisting_ShouldReturnFalse()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);
        file.RootStorage.CreateStream("X", new byte[] { 1 });

        Assert.IsFalse(file.RootStorage.TryOpenStream("X", FileMode.CreateNew, FileAccess.Write, out _));
    }

    /// <summary>
    /// Verifies that the mode/access <c>TryOpenStream</c> returns <see langword="false" /> when the named entry is a
    /// storage rather than a stream.
    /// </summary>
    [TestMethod]
    public void TryOpenStream_WhenEntryIsStorage_ShouldReturnFalse()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);
        _ = file.RootStorage.CreateStorage("S");

        Assert.IsFalse(file.RootStorage.TryOpenStream("S", FileMode.Open, FileAccess.ReadWrite, out _));
    }

    /// <summary>
    /// Verifies that opening an existing file write-only (<see cref="FileMode.Open" /> + <see cref="FileAccess.Write" />)
    /// throws <see cref="NotSupportedException" />: mutating an existing file requires read access.
    /// </summary>
    [TestMethod]
    public void Open_WhenWriteOnlyAndModeOpen_ShouldThrowNotSupportedException()
    {
        MemoryStream destination = CreateContainer(("A", [1]));
        destination.Position = 0;

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            using var _ = CompoundFile.Open(destination, FileMode.Open, FileAccess.Write);
        });
    }

    /// <summary>
    /// Verifies that creating a file write-only is supported and yields a non-readable, writable file.
    /// </summary>
    [TestMethod]
    public void Open_WhenWriteOnlyAndModeCreate_ShouldSucceed()
    {
        using var destination = new MemoryStream();

        using var file = CompoundFile.Open(destination, FileMode.Create, FileAccess.Write, leaveOpen: true);

        Assert.IsTrue(file.CanWrite);
        Assert.IsFalse(file.CanRead);
    }

    /// <summary>
    /// Verifies that reading a disposed <see cref="CompoundStream" /> throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenStreamDisposed_ShouldThrowObjectDisposedException()
    {
        using CompoundFile file = OpenSample();
        string name = file.RootStorage.EnumerateStreams().First().Name;
        CompoundStream stream = file.RootStorage.OpenStream(name);
        stream.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = stream.Read(new byte[1], 0, 1));
    }

    /// <summary>
    /// Verifies that seeking, reading length, and materializing a disposed <see cref="CompoundStream" /> throw
    /// <see cref="ObjectDisposedException" />, and that the capability flags become <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Members_WhenStreamDisposed_ShouldThrowOrReportNoCapability()
    {
        using CompoundFile file = OpenSample();
        string name = file.RootStorage.EnumerateStreams().First().Name;
        CompoundStream stream = file.RootStorage.OpenStream(name);
        stream.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = stream.Seek(0, SeekOrigin.Begin));
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = stream.Length);
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = stream.AsMemory());
        Assert.IsFalse(stream.CanRead);
        Assert.IsFalse(stream.CanSeek);
        Assert.IsFalse(stream.CanWrite);
    }

    /// <summary>
    /// Verifies that setting <see cref="CompoundStream.Position" /> beyond the length is preserved and a subsequent read
    /// returns zero, matching the <see cref="System.IO.Stream" /> contract.
    /// </summary>
    [TestMethod]
    public void Position_WhenSetBeyondLength_ShouldPreservePositionAndReadZero()
    {
        using CompoundFile file = OpenSample();
        string name = file.RootStorage.EnumerateStreams().First().Name;
        using CompoundStream stream = file.RootStorage.OpenStream(name);

        long beyond = stream.Length + 100;
        stream.Position = beyond;

        Assert.AreEqual(beyond, stream.Position);
        Assert.AreEqual(0, stream.Read(new byte[4], 0, 4));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStream.Seek(long, SeekOrigin)" /> beyond the end returns the requested position.
    /// </summary>
    [TestMethod]
    public void Seek_WhenBeyondEnd_ShouldReturnRequestedPosition()
    {
        using CompoundFile file = OpenSample();
        string name = file.RootStorage.EnumerateStreams().First().Name;
        using CompoundStream stream = file.RootStorage.OpenStream(name);

        long target = stream.Length + 50;

        Assert.AreEqual(target, stream.Seek(target, SeekOrigin.Begin));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStream.ReadAllBytes" /> returns a <see cref="byte" /> array.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_ShouldReturnByteArray()
    {
        using CompoundFile file = OpenSample();
        string name = file.RootStorage.EnumerateStreams().First().Name;
        using CompoundStream stream = file.RootStorage.OpenStream(name);

        Assert.IsInstanceOfType<byte[]>(stream.ReadAllBytes());
    }
}
