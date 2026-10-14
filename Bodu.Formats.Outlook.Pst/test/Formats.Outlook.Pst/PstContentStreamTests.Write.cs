// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstContentStreamTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Pst;

public partial class PstContentStreamTests
{
    /// <summary>
    /// Verifies that the stream is read-only: it reports that it cannot write, and a write throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Write_WhenCalled_ShouldThrowNotSupportedException()
    {
        using PstContentStream stream = Over([1, 2, 3, 4]);

        Assert.IsFalse(stream.CanWrite);
        Assert.IsTrue(stream.CanRead);
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            stream.Write([9], 0, 1);
        });
    }
}
