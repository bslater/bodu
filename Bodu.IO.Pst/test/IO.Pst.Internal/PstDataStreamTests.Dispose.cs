// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstDataStreamTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

public partial class PstDataStreamTests
{
    /// <summary>
    /// Verifies that a disposed stream reports itself unreadable and rejects reads and seeks.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldRejectFurtherUse()
    {
        Stream stream = OpenStream(out PstFile file, Payload(100, 1));
        using (file)
        {
            stream.Dispose();

            Assert.IsFalse(stream.CanRead);
            Assert.IsFalse(stream.CanSeek);
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                _ = stream.Read(new byte[1], 0, 1);
            });
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                _ = stream.Seek(0, SeekOrigin.Begin);
            });
        }
    }

    /// <summary>
    /// Verifies that disposing a stream twice is harmless.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        Stream stream = OpenStream(out PstFile file, Payload(10, 1));
        using (file)
        {
            stream.Dispose();
            stream.Dispose();
        }
    }

    /// <summary>
    /// Verifies that a stream outliving its session fails with the session's disposal exception even when the leaf it
    /// needs is already cached, rather than serving stale data or surfacing the underlying stream's error.
    /// </summary>
    [TestMethod]
    public void Read_WhenOwningSessionDisposed_ShouldThrowObjectDisposedException()
    {
        Stream stream = OpenStream(out PstFile file, Payload(100, 1), Payload(100, 2));
        var buffer = new byte[10];
        Assert.AreEqual(10, stream.Read(buffer, 0, buffer.Length));

        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = stream.Read(buffer, 0, buffer.Length);
        });
    }
}
