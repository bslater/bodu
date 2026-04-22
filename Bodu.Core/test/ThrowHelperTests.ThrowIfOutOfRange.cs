// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfOutOfRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Provides test cases for values that should not throw in <see cref="ThrowHelper.ThrowIfOutOfRange{T}" />.
    /// </summary>
    public static IEnumerable<object[]> GetInRangeTestData()
    {
        // inclusive = true
        yield return new object[] { 6, 6, 10, true };   // Equal to lower bound
        yield return new object[] { 8, 6, 10, true };   // Within bounds
        yield return new object[] { 10, 6, 10, true };  // Equal to upper bound
        yield return new object[] { 1, 0, 1, true };    // Equal to upper
        yield return new object[] { 0, 0, 1, true };    // Equal to lower

        // inclusive = false
        yield return new object[] { 7, 6, 10, false };  // Strictly between
        yield return new object[] { 6.5, 6.0, 10.0, false }; // Floating point
    }

    /// <summary>
    /// Provides test cases from <see cref="GetInRangeTestData" /> that use inclusive bounds (i.e., where <c>inclusive == true</c>).
    /// This is used to verify the default behavior of <see cref="ThrowHelper.ThrowIfOutOfRange{T}" />, where the <c>inclusive</c>
    /// parameter defaults to <c>true</c>.
    /// </summary>
    /// <returns>
    /// A filtered sequence of test cases, each represented as an <c>object[]</c> with value, min, and max parameters (omitting the
    /// explicit inclusive flag).
    /// </returns>
    public static IEnumerable<object[]> GetInRangeTestData_UsingDefaultInclusive() =>
        GetInRangeTestData()
            .Where(o => (bool)o[3]) // inclusive == true
            .Select(o => new object[] { o[0], o[1], o[2] });

    /// <summary>
    /// Provides test cases for values that should cause <see cref="ThrowHelper.ThrowIfOutOfRange{T}" /> to throw.
    /// </summary>
    public static IEnumerable<object[]> GetOutOfRangeTestData()
    {
        // inclusive = true
        yield return new object[] { 5, 6, 10, true };   // Below lower bound
        yield return new object[] { 11, 6, 10, true };  // Above upper bound
        yield return new object[] { -1, 0, 1, true };   // Below minimum

        // inclusive = false (exclusive bounds)
        yield return new object[] { 6, 6, 10, false };  // Equal to lower bound
        yield return new object[] { 10, 6, 10, false }; // Equal to upper bound
        yield return new object[] { 5, 5, 5, false };   // Equal to bounds
        yield return new object[] { 4, 4, 4, false };   // All equal
        yield return new object[] { 3, 4, 4, false };   // Below lower
    }

    // value, min, max

    /// <summary>
    /// Provides test cases from <see cref="GetOutOfRangeTestData" /> that use inclusive bounds (i.e., where <c>inclusive == true</c>).
    /// This is used to verify that <see cref="ThrowHelper.ThrowIfOutOfRange{T}" /> throws an exception by default when the value is
    /// outside the inclusive bounds and the 'inclusive' parameter is omitted.
    /// </summary>
    /// <returns>
    /// A filtered sequence of test cases, each represented as an <c>object[]</c> with value, min, and max parameters (omitting the
    /// explicit inclusive flag).
    /// </returns>
    public static IEnumerable<object[]> GetOutOfRangeTestData_UsingDefaultInclusive() =>
        GetOutOfRangeTestData()
            .Where(o => (bool)o[3]) // inclusive == true
            .Select(o => new object[] { o[0], o[1], o[2] }); // value, min, max

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfOutOfRange{T}" /> uses inclusive bounds by default when the 'inclusive' parameter is omitted.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetInRangeTestData_UsingDefaultInclusive), DynamicDataSourceType.Method)]
    public void ThrowIfOutOfRange_WhenCalledWithoutInclusiveParameter_ShouldBehaveAsInclusive<T>(T value, T min, T max)
        where T : IComparable<T>
    {
        // Should not throw
        ThrowHelper.ThrowIfOutOfRange(value, min, max);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfOutOfRange{T}" /> throws an <see cref="ArgumentOutOfRangeException" /> when the
    /// value is outside the inclusive bounds and the 'inclusive' parameter is omitted.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetOutOfRangeTestData_UsingDefaultInclusive), DynamicDataSourceType.Method)]
    public void ThrowIfOutOfRange_WhenCalledWithoutInclusiveParameter_ShouldThrowForValuesOutsideInclusiveRange<T>(T value, T min, T max)
        where T : IComparable<T>
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfOutOfRange(value, min, max);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfOutOfRange{T}" /> throws for values outside the specified range.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetOutOfRangeTestData), DynamicDataSourceType.Method)]
    public void ThrowIfOutOfRange_WhenValueOutsideRange_ShouldThrow<T>(T value, T min, T max, bool inclusive)
        where T : IComparable<T>
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfOutOfRange(value, min, max, inclusive);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfOutOfRange{T}" /> does not throw for values within the specified range.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetInRangeTestData), DynamicDataSourceType.Method)]
    public void ThrowIfOutOfRange_WhenValueWithinRange_ShouldNotThrow<T>(T value, T min, T max, bool inclusive)
        where T : IComparable<T>
    {
        ThrowHelper.ThrowIfOutOfRange(value, min, max, inclusive);
    }
}
