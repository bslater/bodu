// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.MinMax.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Max(DateOnly?, DateOnly?)" /> returns the later non-null value and falls back to the
    /// non-null operand when one input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Max_WhenComparingNullableDates_ShouldHandleNulls()
    {
        DateOnly? earlier = new DateOnly(2024, 1, 1);
        DateOnly? later = new DateOnly(2024, 12, 31);

        Assert.AreEqual(later, DateOnlyExtensions.Max(earlier, later));
        Assert.AreEqual(later, DateOnlyExtensions.Max(later, earlier));
        Assert.AreEqual(later, DateOnlyExtensions.Max(later, (DateOnly?)null));
        Assert.AreEqual(earlier, DateOnlyExtensions.Max((DateOnly?)null, earlier));
        Assert.IsNull(DateOnlyExtensions.Max((DateOnly?)null, (DateOnly?)null));
    }
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Max(DateOnly, DateOnly)" /> returns the later date for non-equal inputs and
    /// <paramref name="first" /> when both dates are equal.
    /// </summary>
    [TestMethod]
    public void Max_WhenComparingTwoDates_ShouldReturnLater()
    {
        var earlier = new DateOnly(2024, 1, 1);
        var later = new DateOnly(2024, 12, 31);

        Assert.AreEqual(later, DateOnlyExtensions.Max(earlier, later));
        Assert.AreEqual(later, DateOnlyExtensions.Max(later, earlier));
        Assert.AreEqual(earlier, DateOnlyExtensions.Max(earlier, earlier));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Min(DateOnly?, DateOnly?)" /> returns the earlier non-null value and falls back to
    /// the non-null operand when one input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Min_WhenComparingNullableDates_ShouldHandleNulls()
    {
        DateOnly? earlier = new DateOnly(2024, 1, 1);
        DateOnly? later = new DateOnly(2024, 12, 31);

        Assert.AreEqual(earlier, DateOnlyExtensions.Min(earlier, later));
        Assert.AreEqual(earlier, DateOnlyExtensions.Min(later, earlier));
        Assert.AreEqual(later, DateOnlyExtensions.Min(later, (DateOnly?)null));
        Assert.AreEqual(earlier, DateOnlyExtensions.Min((DateOnly?)null, earlier));
        Assert.IsNull(DateOnlyExtensions.Min((DateOnly?)null, (DateOnly?)null));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Min(DateOnly, DateOnly)" /> returns the earlier date for non-equal inputs and
    /// <paramref name="first" /> when both dates are equal.
    /// </summary>
    [TestMethod]
    public void Min_WhenComparingTwoDates_ShouldReturnEarlier()
    {
        var earlier = new DateOnly(2024, 1, 1);
        var later = new DateOnly(2024, 12, 31);

        Assert.AreEqual(earlier, DateOnlyExtensions.Min(earlier, later));
        Assert.AreEqual(earlier, DateOnlyExtensions.Min(later, earlier));
        Assert.AreEqual(later, DateOnlyExtensions.Min(later, later));
    }

}
