// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test;

namespace Bodu.IO.Compound;

public partial class CompoundStreamTests
{
    /// <summary>
    /// Verifies that a write that would grow the payload beyond <see cref="int.MaxValue" /> throws
    /// <see cref="NotSupportedException" /> before allocating.
    /// </summary>
    [TestMethod]
    public void Write_WhenGrowingBeyondInt32MaxValue_ShouldThrowNotSupportedException()
    {
        using var destination = new MemoryStream();
        using var file = CompoundFile.Create(destination, leaveOpen: true);
        using CompoundStream cursor = file.RootStorage.CreateStream("Data");

        cursor.Position = int.MaxValue - 1;

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            cursor.Write([1, 2]);
        });
    }

    /// <summary>
    /// Verifies that a multi-hundred-megabyte payload written through the cursor commits and reads back intact
    /// through a streaming reopen.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Write_WhenMultiHundredMegabytePayload_ShouldCommitAndReadBackIntact()
    {
        const int ChunkSize = 1 << 20;
        const int ChunkCount = 256;
        string path = Path.Combine(Path.GetTempPath(), $"bodu-compound-{Guid.NewGuid():N}.cfb");

        try
        {
            byte[] chunk = new byte[ChunkSize];
            byte[] writtenHash;

            using (var incremental = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                using (var file = CompoundFile.Create(path))
                {
                    using (CompoundStream cursor = file.RootStorage.CreateStream("Payload"))
                    {
                        for (int i = 0; i < ChunkCount; i++)
                        {
                            chunk.AsSpan().Fill((byte)(i + 1));
                            incremental.AppendData(chunk);
                            cursor.Write(chunk);
                        }
                    }

                    file.Commit();
                }

                writtenHash = incremental.GetHashAndReset();
            }

            using var source = File.OpenRead(path);
            using var reopened = CompoundFile.Open(source, leaveOpen: true, buffered: false);
            using CompoundStream read = reopened.RootStorage.OpenStream("Payload");

            Assert.AreEqual((long)ChunkSize * ChunkCount, read.Length);

            using var verify = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] buffer = new byte[ChunkSize];
            int count;
            while ((count = read.Read(buffer, 0, buffer.Length)) > 0)
                verify.AppendData(buffer.AsSpan(0, count));

            CollectionAssert.AreEqual(writtenHash, verify.GetHashAndReset());
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that every synchronous write overload of a cursor opened for reading throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Write_WhenCursorIsReadOnly_ShouldThrowNotSupportedException()
    {
        byte[] container = BuildContainer(out _);
        using var file = CompoundFile.Open(new MemoryStream(container));
        using CompoundStream stream = file.RootStorage.OpenStream("Data");

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            stream.WriteByte(1);
        });
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            stream.Write(new byte[] { 1 }, 0, 1);
        });
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            stream.Write((ReadOnlySpan<byte>)new byte[] { 1 });
        });
    }

    /// <summary>
    /// Verifies that every synchronous write overload of a writable cursor throws <see cref="ObjectDisposedException" />
    /// naming the compound file once the file that owns the cursor has been disposed, since the bytes could never be
    /// committed, and leaves the cursor's bytes as they were.
    /// </summary>
    [TestMethod]
    public void Write_WhenOwningFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        using CompoundStream cursor = OpenCursorOverDisposedFile();

        ObjectDisposedException ex = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cursor.Write((ReadOnlySpan<byte>)new byte[] { 4 });
        });
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cursor.Write(new byte[] { 4 }, 0, 1);
        });
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cursor.WriteByte(4);
        });

        Assert.AreEqual(typeof(CompoundFile).FullName, ex.ObjectName);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, cursor.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that an asynchronous write to a writable cursor throws <see cref="ObjectDisposedException" /> once the
    /// file that owns the cursor has been disposed.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task WriteAsync_WhenOwningFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        using CompoundStream cursor = OpenCursorOverDisposedFile();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
        {
            await cursor.WriteAsync(new byte[] { 4 }, 0, 1);
        });
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
        {
            await cursor.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 4 }));
        });
    }

    /// <summary>
    /// Verifies that a committed stream grown through the writable cursor between two lengths that sit either side
    /// of, at, or past the mini-stream cutoff, and committed again, is committed intact: the stream moves from the
    /// mini stream, or from empty, to the regular sectors where the lengths straddle the cutoff, and stays in the
    /// regular sectors where both are at or above it; a strict reopen accepts the container, reads back exactly the
    /// bytes written, and finds the stream beside it unchanged.
    /// </summary>
    /// <param name="before">The length of the stream as first committed.</param>
    /// <param name="after">The length written through the cursor before the second commit.</param>
    [TestMethod]
    [DataRow(MiniStreamCutoff - 1, MiniStreamCutoff)]
    [DataRow(MiniStreamCutoff, MiniStreamCutoff + 1)]
    [DataRow(MiniStreamCutoff - 1, MiniStreamCutoff + 1)]
    [DataRow(0, MiniStreamCutoff)]
    [DataRow(63, 2 * MiniStreamCutoff)]
    public void Write_WhenGrowingAcrossTheMiniStreamCutoff_ShouldCommitAndReadBackIntact(int before, int after)
    {
        using MemoryStream destination = CreateCommittedContainer(before);
        byte[] edited = CompoundContainerBytes.Pattern(after, EditedPatternStart);

        using (var file = CompoundFile.Open(destination, FileMode.Open, FileAccess.ReadWrite, leaveOpen: true))
        {
            using (CompoundStream cursor = file.RootStorage.OpenStream("Data", FileMode.Open, FileAccess.ReadWrite))
                cursor.Write(edited);

            file.Commit();
        }

        AssertCommittedStreams(destination.ToArray(), edited);
    }
}
