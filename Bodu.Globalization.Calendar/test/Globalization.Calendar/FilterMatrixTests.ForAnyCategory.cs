// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterMatrixTests.ForAnyCategory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> throws <see cref="ArgumentNullException" /> when the
    /// categories array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ForAnyCategory_WhenCategoriesIsNull_ShouldThrowExactly()
    {
        NotableDateCategory[] categories = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.ForAnyCategory(categories);
        });
    }
}
