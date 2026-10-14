// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstContentStreamTests.SetLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Pst;

public partial class PstContentStreamTests
{
    /// <summary>
    /// Verifies that changing the length throws <see cref="NotSupportedException" />, the stream being read-only.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenCalled_ShouldThrowNotSupportedException()
    {
        using PstContentStream stream = Over([1, 2, 3, 4]);

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            stream.SetLength(1);
        });
    }
}
