// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that disposing a compound file twice does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        CompoundFile file = OpenSample();

        file.Dispose();
        file.Dispose();
    }

    /// <summary>
    /// Verifies that opening a stream after the file has been disposed throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenStreamOpenedAfterDispose_ShouldThrowObjectDisposedException()
    {
        CompoundFile file = OpenSample();
        string name = file.RootStorage.EnumerateStreams().First().Name;
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => file.RootStorage.OpenStream(name));
    }

    /// <summary>
    /// Verifies that disposing a buffered file opened without <c>leaveOpen</c> disposes its source stream.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenLeaveOpenIsFalse_ShouldDisposeSourceStream()
    {
        var source = new MemoryStream(CreateContainer(("A", CompoundContainerBytes.Pattern(100))).ToArray());
        CompoundFile file = CompoundFile.Open(source);

        file.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = source.ReadByte();
        });
    }

    /// <summary>
    /// Verifies that disposing a file opened for update with <c>leaveOpen</c> after a commit faulted part way through
    /// the write neither throws nor touches the destination again: no further write or flush reaches it, and its
    /// length is what the faulted commit left.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCommitFaultedMidWrite_ShouldWriteNothingMore()
    {
        using var destination = new FaultingWriteStream(CreateSeedContainer(), MidWriteFaultBytes);
        CompoundFile file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true);
        StageSampleEdits(file);
        Assert.ThrowsExactly<IOException>(() =>
        {
            file.Commit();
        });

        var writeCount = destination.WriteCount;
        var flushCount = destination.FlushCount;
        var length = destination.Length;

        file.Dispose();

        Assert.AreEqual(writeCount, destination.WriteCount);
        Assert.AreEqual(flushCount, destination.FlushCount);
        Assert.AreEqual(length, destination.Length);
    }
}
