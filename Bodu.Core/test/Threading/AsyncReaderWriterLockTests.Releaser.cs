// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLockTests.Releaser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncReaderWriterLockTests
{
    /// <summary>
    /// Verifies that disposing a default <see cref="AsyncReaderWriterLock.Releaser" /> (one with no owner) is a
    /// harmless no-op.
    /// </summary>
    [TestMethod]
    public void Releaser_WhenDefault_ShouldDisposeWithoutThrowing()
    {
        var releaser = default(AsyncReaderWriterLock.Releaser);

        releaser.Dispose();
    }

    /// <summary>
    /// Verifies that disposing a reader releaser releases read access, allowing a writer to proceed.
    /// </summary>
    [TestMethod]
    public async Task Releaser_WhenReaderDisposed_ShouldReleaseReadAccess()
    {
        var sut = new AsyncReaderWriterLock();
        var reader = await sut.ReaderAsync();

        var writer = sut.WriterAsync();
        Assert.IsFalse(writer.IsCompleted);

        reader.Dispose();

        (await writer).Dispose();
    }

    /// <summary>
    /// Verifies that disposing a writer releaser releases write access, allowing a reader to proceed.
    /// </summary>
    [TestMethod]
    public async Task Releaser_WhenWriterDisposed_ShouldReleaseWriteAccess()
    {
        var sut = new AsyncReaderWriterLock();
        var writer = await sut.WriterAsync();

        var reader = sut.ReaderAsync();
        Assert.IsFalse(reader.IsCompleted);

        writer.Dispose();

        (await reader).Dispose();
    }

    /// <summary>
    /// Verifies that disposing a reader releaser twice releases read access only once, so a second active reader still
    /// defers a waiting writer.
    /// </summary>
    [TestMethod]
    public async Task Releaser_WhenReaderDisposedTwice_ShouldNotUnderflowReaderCount()
    {
        var sut = new AsyncReaderWriterLock();
        var reader1 = await sut.ReaderAsync();
        var reader2 = await sut.ReaderAsync();

        var writer = sut.WriterAsync();
        Assert.IsFalse(writer.IsCompleted);

        reader1.Dispose();
        reader1.Dispose();

        // The second reader still holds read access, so the writer must keep waiting.
        Assert.IsFalse(writer.IsCompleted, "Double-dispose must not release the second reader's access.");

        reader2.Dispose();

        (await writer).Dispose();
    }

    /// <summary>
    /// Verifies that disposing a copy of a reader releaser is idempotent: only the first dispose releases access.
    /// </summary>
    [TestMethod]
    public async Task Releaser_WhenCopiedReaderDisposed_ShouldReleaseAccessOnce()
    {
        var sut = new AsyncReaderWriterLock();
        var reader = await sut.ReaderAsync();
        var reader2 = await sut.ReaderAsync();
        var copy = reader;

        var writer = sut.WriterAsync();
        Assert.IsFalse(writer.IsCompleted);

        reader.Dispose();
        copy.Dispose();

        Assert.IsFalse(writer.IsCompleted, "Disposing the copy must not release a second time.");

        reader2.Dispose();

        (await writer).Dispose();
    }

    /// <summary>
    /// Verifies that disposing a writer releaser twice grants write access to only one queued writer, never two.
    /// </summary>
    [TestMethod]
    public async Task Releaser_WhenWriterDisposedTwice_ShouldNotGrantOverlappingWriters()
    {
        var sut = new AsyncReaderWriterLock();
        var writer1 = await sut.WriterAsync();
        var writer2 = sut.WriterAsync();
        var writer3 = sut.WriterAsync();

        writer1.Dispose();
        writer1.Dispose();

        var granted2 = await writer2;

        // The third writer must remain queued; a double-dispose must not grant it concurrently.
        Assert.IsFalse(writer3.IsCompleted, "Double-dispose must not grant a second writer.");

        granted2.Dispose();

        (await writer3).Dispose();
    }
}
