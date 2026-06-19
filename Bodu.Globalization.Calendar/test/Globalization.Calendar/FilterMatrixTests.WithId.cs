// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterMatrixTests.WithId.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithId" /> throws <see cref="ArgumentNullException" /> when the id is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithId_WhenNotableDateIdIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithId(null!);
        });
    }
}
