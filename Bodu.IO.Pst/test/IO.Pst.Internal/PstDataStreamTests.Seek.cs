// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstDataStreamTests.Seek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

public partial class PstDataStreamTests
{
    /// <summary>
    /// Verifies that seeking from each origin lands where the <see cref="Stream" /> contract says.
    /// </summary>
    [TestMethod]
    public void Seek_WhenEachOrigin_ShouldPositionAccordingly()
    {
        Stream stream = OpenStream(out PstFile file, Payload(100, 1));
        using (file)
        using (stream)
        {
            Assert.AreEqual(40L, stream.Seek(40, SeekOrigin.Begin));
            Assert.AreEqual(45L, stream.Seek(5, SeekOrigin.Current));
            Assert.AreEqual(90L, stream.Seek(-10, SeekOrigin.End));
            Assert.AreEqual(90L, stream.Position);
        }
    }

    /// <summary>
    /// Verifies that a position beyond the end reads nothing rather than failing.
    /// </summary>
    [TestMethod]
    public void Seek_WhenPastEnd_ShouldReadZeroBytes()
    {
        Stream stream = OpenStream(out PstFile file, Payload(100, 1));
        using (file)
        using (stream)
        {
            Assert.AreEqual(150L, stream.Seek(50, SeekOrigin.End));
            Assert.AreEqual(0, stream.Read(new byte[8], 0, 8));
        }
    }

    /// <summary>
    /// Verifies that a seek before the start is rejected.
    /// </summary>
    [TestMethod]
    public void Seek_WhenBeforeStart_ShouldThrowArgumentOutOfRangeException()
    {
        Stream stream = OpenStream(out PstFile file, Payload(100, 1));
        using (file)
        using (stream)
        {
            _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = stream.Seek(-1, SeekOrigin.Begin);
            });
        }
    }

    /// <summary>
    /// Verifies that an undefined origin is rejected.
    /// </summary>
    [TestMethod]
    public void Seek_WhenOriginUndefined_ShouldThrowArgumentException()
    {
        Stream stream = OpenStream(out PstFile file, Payload(100, 1));
        using (file)
        using (stream)
        {
            _ = Assert.ThrowsExactly<ArgumentException>(() =>
            {
                _ = stream.Seek(0, (SeekOrigin)7);
            });
        }
    }

    /// <summary>
    /// Verifies that the write-side members are unsupported on the read-only stream.
    /// </summary>
    [TestMethod]
    public void Write_WhenCalled_ShouldThrowNotSupportedException()
    {
        Stream stream = OpenStream(out PstFile file, Payload(10, 1));
        using (file)
        using (stream)
        {
            Assert.IsFalse(stream.CanWrite);
            _ = Assert.ThrowsExactly<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
            _ = Assert.ThrowsExactly<NotSupportedException>(() => stream.SetLength(1));
        }
    }
}
