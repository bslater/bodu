// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterCombinatorTests.AllOf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterCombinatorTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> throws <see cref="ArgumentNullException" /> when the filters
    /// array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AllOf_WhenFiltersIsNull_ShouldThrowExactly()
    {
        NotableDateFilter[] filters = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.AllOf(filters);
        });
    }
}
