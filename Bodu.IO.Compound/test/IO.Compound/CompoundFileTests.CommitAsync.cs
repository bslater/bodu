// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.CommitAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Builds a small, deterministic writable tree used by the asynchronous-commit tests.
    /// </summary>
    /// <param name="destination">The destination the file is created over.</param>
    /// <returns>The writable compound file, positioned for commit.</returns>
    private static CompoundFile CreateSampleTree(Stream destination)
    {
        var file = CompoundFile.Create(destination, leaveOpen: true);
        file.RootStorage.CreateStream("Meta", new byte[] { 1, 2, 3, 4 });
        CompoundStorage data = file.RootStorage.CreateStorage("Data");
        data.CreateStream("Large", CreatePayload(6000));
        data.CreateStream("Small", CreatePayload(64));
        return file;
    }

    /// <summary>
    /// Generates a deterministic payload of the requested length.
    /// </summary>
    /// <param name="size">The payload length.</param>
    /// <returns>The payload.</returns>
    private static byte[] CreatePayload(int size)
    {
        byte[] payload = new byte[size];
        for (int i = 0; i < size; i++)
            payload[i] = (byte)((i * 17) + 3);

        return payload;
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.CommitAsync" /> writes exactly the same bytes as the synchronous
    /// <see cref="CompoundFile.Commit" />.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public async Task CommitAsync_WhenWritable_ShouldProduceSameBytesAsCommit()
    {
        using var syncDestination = new MemoryStream();
        using (CompoundFile file = CreateSampleTree(syncDestination))
            file.Commit();

        using var asyncDestination = new MemoryStream();
        using (CompoundFile file = CreateSampleTree(asyncDestination))
            await file.CommitAsync();

        CollectionAssert.AreEqual(syncDestination.ToArray(), asyncDestination.ToArray());
    }

    /// <summary>
    /// Verifies that the committed container reopens and round-trips its streams.
    /// </summary>
    [TestMethod]
    public async Task CommitAsync_WhenWritable_ShouldRoundTripThroughReopen()
    {
        var destination = new MemoryStream();

        using (CompoundFile file = CreateSampleTree(destination))
        {
            await file.CommitAsync();
            Assert.IsFalse(file.IsDirty);
        }

        destination.Position = 0;
        using var reopened = CompoundFile.Open(destination);
        using CompoundStream large = reopened.RootStorage.OpenStorage("Data").OpenStream("Large");

        CollectionAssert.AreEqual(CreatePayload(6000), large.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.CommitAsync" /> on a read-only file throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public async Task CommitAsync_WhenReadOnly_ShouldThrowInvalidOperationException()
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/sample1.doc"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await file.CommitAsync());
    }

    /// <summary>
    /// Verifies that a token already canceled throws before any byte is written to the destination.
    /// </summary>
    [TestMethod]
    public async Task CommitAsync_WhenCancelledBeforeWrite_ShouldWriteNothing()
    {
        var destination = new MemoryStream();

        using (CompoundFile file = CreateSampleTree(destination))
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await file.CommitAsync(cts.Token));
        }

        Assert.AreEqual(0, destination.Length);
    }

    /// <summary>
    /// Verifies that committing asynchronously to a non-seekable destination appends the same bytes a seekable
    /// commit would produce.
    /// </summary>
    [TestMethod]
    public async Task CommitAsync_WhenDestinationNotSeekable_ShouldAppendCommittedBytes()
    {
        byte[] expected;
        using (var seekable = new MemoryStream())
        {
            using CompoundFile file = CreateSampleTree(seekable);
            await file.CommitAsync();
            expected = seekable.ToArray();
        }

        using var sink = new NonSeekableWriteStream();
        using (CompoundFile file = CreateSampleTree(sink))
            await file.CommitAsync();

        CollectionAssert.AreEqual(expected, sink.ToArray());
    }

    /// <summary>
    /// A minimal writable, non-seekable stream that captures everything written to it.
    /// </summary>
    private sealed class NonSeekableWriteStream
        : Stream
    {
        /// <summary>The backing store for the written bytes.</summary>
        private readonly MemoryStream _inner = new();

        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Returns the bytes written so far.
        /// </summary>
        /// <returns>The captured bytes.</returns>
        public byte[] ToArray() => _inner.ToArray();

        /// <inheritdoc />
        public override void Flush() => _inner.Flush();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        /// <inheritdoc />
        public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);

        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.WriteAsync(buffer, cancellationToken);

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();

            base.Dispose(disposing);
        }
    }
}
