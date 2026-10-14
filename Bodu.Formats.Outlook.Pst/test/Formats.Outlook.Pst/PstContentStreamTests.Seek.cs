// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstContentStreamTests.Seek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Pst;

public partial class PstContentStreamTests
{
    /// <summary>
    /// Verifies that seeking moves the wrapped stream, so that the next read starts at the new position, and that the
    /// stream reports the wrapped stream's seekability.
    /// </summary>
    [TestMethod]
    public void Seek_WhenInnerStreamIsSeekable_ShouldMoveTheWrappedStream()
    {
        using PstContentStream stream = Over([1, 2, 3, 4]);

        long position = stream.Seek(-1, SeekOrigin.End);

        Assert.IsTrue(stream.CanSeek);
        Assert.AreEqual(3, position);
        Assert.AreEqual(4, stream.ReadByte());
    }
}
