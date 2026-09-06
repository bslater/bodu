// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstDataStreamTests.ReadAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

public partial class PstDataStreamTests
{
    /// <summary>
    /// Verifies that the memory-based asynchronous read yields the same bytes as the synchronous read across leaf
    /// boundaries.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WhenMemoryBuffer_ShouldMatchSynchronousRead()
    {
        byte[] first = Payload(150, 3);
        byte[] second = Payload(70, 4);
        Stream stream = OpenStream(out PstFile file, first, second);
        using (file)
        using (stream)
        {
            var buffer = new byte[220];
            int total = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(total, Math.Min(64, buffer.Length - total)))) > 0)
                total += read;

            Assert.AreEqual(220, total);
            CollectionAssert.AreEqual((byte[])[.. first, .. second], buffer);
        }
    }

    /// <summary>
    /// Verifies that the array-based asynchronous read yields the same bytes as the synchronous read.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WhenArrayBuffer_ShouldMatchSynchronousRead()
    {
        byte[] payload = Payload(300, 5);
        Stream stream = OpenStream(out PstFile file, payload);
        using (file)
        using (stream)
        {
            var buffer = new byte[300];
            int total = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer, total, buffer.Length - total)) > 0)
                total += read;

            Assert.AreEqual(300, total);
            CollectionAssert.AreEqual(payload, buffer);
        }
    }

    /// <summary>
    /// Verifies that an already-cancelled token cancels the read before any byte is produced.
    /// </summary>
    [TestMethod]
    public async Task ReadAsync_WhenTokenCancelled_ShouldCancel()
    {
        Stream stream = OpenStream(out PstFile file, Payload(10, 6));
        using (file)
        using (stream)
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
            {
                _ = await stream.ReadAsync(new byte[4].AsMemory(), cancellation.Token);
            });
            Assert.AreEqual(0L, stream.Position);
        }
    }

    /// <summary>
    /// Verifies that the asynchronous copy delivers the whole payload.
    /// </summary>
    [TestMethod]
    public async Task CopyToAsync_WhenCalled_ShouldCopyWholePayload()
    {
        byte[] first = Payload(500, 7);
        byte[] second = Payload(501, 8);
        Stream stream = OpenStream(out PstFile file, first, second);
        using (file)
        using (stream)
        {
            var target = new MemoryStream();
            await stream.CopyToAsync(target);

            CollectionAssert.AreEqual((byte[])[.. first, .. second], target.ToArray());
        }
    }
}
