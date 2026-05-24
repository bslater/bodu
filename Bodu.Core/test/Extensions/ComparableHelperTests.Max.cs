// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComparableHelperTests.Max.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class ComparableHelperTests
{

    /// <summary>
    /// Verifies that Max returns the larger of two <see cref="SimpleTestObject" /> values, handling nulls appropriately.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetMaxTestData))]
    public void Max_WhenComparingTwoValues_ShouldReturnLarger(SimpleTestObject? first, SimpleTestObject? second, SimpleTestObject? expected)
    {
        SimpleTestObject? actual = ComparableHelper.Max(first, second);
        Assert.AreEqual(expected, actual);
    }
    private static IEnumerable<object?[]> GetMaxTestData()
    {
        yield return new object?[] { new SimpleTestObject(1), new SimpleTestObject(2), new SimpleTestObject(2) };
        yield return new object?[] { new SimpleTestObject(5), new SimpleTestObject(5), new SimpleTestObject(5) };
        yield return new object?[] { new SimpleTestObject(10), new SimpleTestObject(3), new SimpleTestObject(10) };
        yield return new object?[] { null, new SimpleTestObject(7), new SimpleTestObject(7) };
        yield return new object?[] { new SimpleTestObject(8), null, new SimpleTestObject(8) };
        yield return new object?[] { null, null, null };
    }

}
