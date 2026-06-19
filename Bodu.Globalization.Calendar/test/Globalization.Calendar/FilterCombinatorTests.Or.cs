// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterCombinatorTests.Or.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterCombinatorTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> throws <see cref="ArgumentNullException" /> when the sibling is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Or_WhenOtherIsNull_ShouldThrowExactly()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.Or(null!);
        });
    }
}
