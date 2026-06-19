// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterMatrixTests.WithAnyName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> throws <see cref="ArgumentNullException" /> when the
    /// names array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithAnyName_WhenNamesIsNull_ShouldThrowExactly()
    {
        string[] names = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithAnyName(names);
        });
    }
}
