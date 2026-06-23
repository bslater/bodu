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
}
