// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterMatrixTests.WithTag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> throws <see cref="ArgumentNullException" /> when the tag is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithTag_WhenTagIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithTag(null!);
        });
    }
}
