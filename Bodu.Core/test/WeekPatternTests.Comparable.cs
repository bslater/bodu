// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Comparable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{

    /// <summary>
    /// Provides object values of types incompatible with <see cref="WeekPattern.CompareTo(object)" />.
    /// </summary>
    public static IEnumerable<object[]> InvalidTypesForCompareTo =>
    [
        ["invalid"],
        [DateTime.Now],
        [new()]
    ];

    /// <summary>
    /// Verifies that <see cref="WeekPattern.CompareTo(byte)" /> returns a value whose sign correctly
    /// reflects the ordinal relationship between the instance and a raw bitmask.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0, (byte)0, 0)]
    [DataRow((byte)1, (byte)0, 1)]
    [DataRow((byte)0, (byte)1, -1)]
    [DataRow((byte)10, (byte)10, 0)]
    [DataRow((byte)127, (byte)5, 1)]
    [DataRow((byte)5, (byte)127, -1)]
    public void CompareToByte_WhenComparing_ShouldReturnCorrectSign(byte first, byte second, int expectedSign)
    {
        Assert.AreEqual(
            Math.Sign(expectedSign),
            Math.Sign(WeekPattern.FromByte(first).CompareTo(second)));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.CompareTo(object)" /> returns a value whose sign correctly
    /// reflects the ordinal relationship when the argument is a boxed <see cref="byte" />.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0, (byte)0, 0)]
    [DataRow((byte)1, (byte)0, 1)]
    [DataRow((byte)0, (byte)1, -1)]
    [DataRow((byte)5, (byte)5, 0)]
    public void CompareToObject_WhenByte_ShouldCompareCorrectly(byte first, byte second, int expectedSign)
    {
        var pattern = WeekPattern.FromByte(first);
        Assert.AreEqual(Math.Sign(expectedSign), Math.Sign(pattern.CompareTo((object)second)));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.CompareTo(object)" /> throws <see cref="ArgumentException" />
    /// when the argument is neither a <see cref="WeekPattern" /> nor a <see cref="byte" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekPatternTests.InvalidTypesForCompareTo), typeof(WeekPatternTests))]
    public void CompareToObject_WhenInvalidType_ShouldThrowExactly(object value)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new WeekPattern(DayOfWeek.Monday).CompareTo(value));
    }
    /// <summary>
    /// Verifies that <see cref="WeekPattern.CompareTo(object)" /> returns a positive value when the
    /// argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CompareToObject_WhenNull_ShouldReturnGreaterThanZero() =>
        Assert.IsGreaterThan(0, new WeekPattern(DayOfWeek.Monday).CompareTo(null));

    /// <summary>
    /// Verifies that <see cref="WeekPattern.CompareTo(object)" /> returns a value whose sign correctly
    /// reflects the ordinal relationship when the argument is a boxed <see cref="WeekPattern" />.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0, (byte)0, 0)]
    [DataRow((byte)1, (byte)0, 1)]
    [DataRow((byte)0, (byte)1, -1)]
    [DataRow((byte)5, (byte)5, 0)]
    [DataRow((byte)3, (byte)7, -1)]
    [DataRow((byte)7, (byte)3, 1)]
    public void CompareToObject_WhenValidWeekPattern_ShouldCompareCorrectly(byte first, byte second, int expectedSign)
    {
        var p1 = WeekPattern.FromByte(first);
        var p2 = WeekPattern.FromByte(second);
        Assert.AreEqual(Math.Sign(expectedSign), Math.Sign(p1.CompareTo((object)p2)));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.CompareTo(WeekPattern)" /> returns a value whose sign
    /// correctly reflects the ordinal relationship between two instances.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0, (byte)0, 0)]
    [DataRow((byte)1, (byte)0, 1)]
    [DataRow((byte)0, (byte)1, -1)]
    [DataRow((byte)7, (byte)7, 0)]
    public void CompareToWeekPattern_WhenComparing_ShouldReturnCorrectSign(byte first, byte second, int expectedSign)
    {
        Assert.AreEqual(
            Math.Sign(expectedSign),
            Math.Sign(WeekPattern.FromByte(first).CompareTo(WeekPattern.FromByte(second))));
    }

}
