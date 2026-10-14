// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstContentStreamTests.Length.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Pst;

public partial class PstContentStreamTests
{
    /// <summary>
    /// Verifies that the length is the wrapped stream's length.
    /// </summary>
    [TestMethod]
    public void Length_WhenInnerStreamIsSeekable_ShouldReturnItsLength()
    {
        using PstContentStream stream = Over([1, 2, 3, 4, 5]);

        Assert.AreEqual(5, stream.Length);
    }
}
