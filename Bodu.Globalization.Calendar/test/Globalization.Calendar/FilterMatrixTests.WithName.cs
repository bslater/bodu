// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterMatrixTests.WithName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> throws <see cref="ArgumentNullException" /> when the name
    /// is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithName_WhenNameIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithName(null!);
        });
    }
}
