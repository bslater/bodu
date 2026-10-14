// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstContentStreamTests.Position.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Pst;

public partial class PstContentStreamTests
{
    /// <summary>
    /// Verifies that setting the position moves the wrapped stream and that reading it back returns the new position.
    /// </summary>
    [TestMethod]
    public void Position_WhenSet_ShouldMoveTheWrappedStream()
    {
        using PstContentStream stream = Over([1, 2, 3, 4]);

        stream.Position = 2;

        Assert.AreEqual(2, stream.Position);
        Assert.AreEqual(3, stream.ReadByte());
    }
}
