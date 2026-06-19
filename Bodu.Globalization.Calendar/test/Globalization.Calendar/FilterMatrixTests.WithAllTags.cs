// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterMatrixTests.WithAllTags.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> throws <see cref="ArgumentNullException" /> when the
    /// tags array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithAllTags_WhenTagsIsNull_ShouldThrowExactly()
    {
        string[] tags = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithAllTags(tags);
        });
    }
}
