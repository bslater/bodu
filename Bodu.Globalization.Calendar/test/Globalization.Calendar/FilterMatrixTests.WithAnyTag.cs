// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterMatrixTests.WithAnyTag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> throws <see cref="ArgumentNullException" /> when the tags
    /// array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithAnyTag_WhenTagsIsNull_ShouldThrowExactly()
    {
        string[] tags = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithAnyTag(tags);
        });
    }
}
