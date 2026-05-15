// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.MinMax.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Max(DateTime?, DateTime?)" /> handles nullable comparisons correctly.
    /// </summary>
    [TestMethod]
    public void Max_WhenComparingNullableDateTimes_ShouldHandleNulls()
    {
        DateTime? earlier = new DateTime(2024, 1, 1);
        DateTime? later = new DateTime(2024, 12, 31);

        Assert.AreEqual(later, DateTimeExtensions.Max(earlier, later));
        Assert.AreEqual(later, DateTimeExtensions.Max(later, earlier));
        Assert.AreEqual(later, DateTimeExtensions.Max(later, (DateTime?)null));
        Assert.AreEqual(earlier, DateTimeExtensions.Max((DateTime?)null, earlier));
        Assert.IsNull(DateTimeExtensions.Max((DateTime?)null, (DateTime?)null));
    }
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Max(DateTime, DateTime)" /> returns the later <see cref="DateTime" />.
    /// </summary>
    [TestMethod]
    public void Max_WhenComparingTwoDateTimes_ShouldReturnLater()
    {
        var earlier = new DateTime(2024, 1, 1, 8, 0, 0);
        var later = new DateTime(2024, 12, 31, 17, 30, 0);

        Assert.AreEqual(later, DateTimeExtensions.Max(earlier, later));
        Assert.AreEqual(later, DateTimeExtensions.Max(later, earlier));
        Assert.AreEqual(later, DateTimeExtensions.Max(later, later));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Min(DateTime?, DateTime?)" /> handles nullable comparisons correctly.
    /// </summary>
    [TestMethod]
    public void Min_WhenComparingNullableDateTimes_ShouldHandleNulls()
    {
        DateTime? earlier = new DateTime(2024, 1, 1);
        DateTime? later = new DateTime(2024, 12, 31);

        Assert.AreEqual(earlier, DateTimeExtensions.Min(earlier, later));
        Assert.AreEqual(earlier, DateTimeExtensions.Min(later, earlier));
        Assert.AreEqual(later, DateTimeExtensions.Min(later, (DateTime?)null));
        Assert.AreEqual(earlier, DateTimeExtensions.Min((DateTime?)null, earlier));
        Assert.IsNull(DateTimeExtensions.Min((DateTime?)null, (DateTime?)null));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Min(DateTime, DateTime)" /> returns the earlier <see cref="DateTime" />.
    /// </summary>
    [TestMethod]
    public void Min_WhenComparingTwoDateTimes_ShouldReturnEarlier()
    {
        var earlier = new DateTime(2024, 1, 1, 8, 0, 0);
        var later = new DateTime(2024, 12, 31, 17, 30, 0);

        Assert.AreEqual(earlier, DateTimeExtensions.Min(earlier, later));
        Assert.AreEqual(earlier, DateTimeExtensions.Min(later, earlier));
        Assert.AreEqual(earlier, DateTimeExtensions.Min(earlier, earlier));
    }

}
