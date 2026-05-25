// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfOutOfRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// This is used to verify the default behaviour of <see cref="ThrowHelper.ThrowIfOutOfRange{T}" />, where the <c>inclusive</c>
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
    [DynamicData(nameof(GetInRangeTestData_UsingDefaultInclusive))]
    public void ThrowIfOutOfRange_WhenCalledWithoutInclusiveParameter_ShouldBehaveAsInclusive<T>(T value, T min, T max)
        where T : IComparable<T> =>
        // Should not throw
        ThrowHelper.ThrowIfOutOfRange(value, min, max);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfOutOfRange{T}" /> throws an <see cref="ArgumentOutOfRangeException" /> when the
    /// value is outside the inclusive bounds and the 'inclusive' parameter is omitted.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetOutOfRangeTestData_UsingDefaultInclusive))]
    public void ThrowIfOutOfRange_WhenCalledWithoutInclusiveParameter_ShouldThrowExactly<T>(T value, T min, T max)
        where T : IComparable<T>
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfOutOfRange(value, min, max);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfOutOfRange{T}(T, T, T, bool, string)" /> does not throw —
    /// and on the ParamName-asserting overload reports nothing — for values inside the requested bounds.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value compared against the bounds.</param>
    /// <param name="min">The minimum bound.</param>
    /// <param name="max">The maximum bound.</param>
    /// <param name="inclusive">Whether the bounds are inclusive.</param>
    [TestMethod]
    [DataRow("inclusive: equal lower", 6, 6, 10, true)]
    [DataRow("inclusive: equal upper", 10, 6, 10, true)]
    [DataRow("exclusive: strictly between", 7, 6, 10, false)]
    public void ThrowIfOutOfRange_WhenValueIsWithinBounds_ShouldNotThrowAndReportNothing(
        string testName, int value, int min, int max, bool inclusive) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfOutOfRange(value, min, max, inclusive, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfOutOfRange{T}(T, T, T, bool, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for values outside the
    /// requested bounds, including boundary equality under exclusive mode.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value compared against the bounds.</param>
    /// <param name="min">The minimum bound.</param>
    /// <param name="max">The maximum bound.</param>
    /// <param name="inclusive">Whether the bounds are inclusive.</param>
    [TestMethod]
    [DataRow("inclusive: below lower", 5, 6, 10, true)]
    [DataRow("inclusive: above upper", 11, 6, 10, true)]
    [DataRow("exclusive: equal lower", 6, 6, 10, false)]
    [DataRow("exclusive: equal upper", 10, 6, 10, false)]
    public void ThrowIfOutOfRange_WhenValueIsOutsideBounds_ShouldThrowOnValue(
        string testName, int value, int min, int max, bool inclusive) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfOutOfRange(value, min, max, inclusive, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfOutOfRange{T}" /> throws for values outside the specified range.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetOutOfRangeTestData))]
    public void ThrowIfOutOfRange_WhenValueOutsideRange_ShouldThrowExactly<T>(T value, T min, T max, bool inclusive)
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
    [DynamicData(nameof(GetInRangeTestData))]
    public void ThrowIfOutOfRange_WhenValueWithinRange_ShouldNotThrow<T>(T value, T min, T max, bool inclusive)
        where T : IComparable<T> => ThrowHelper.ThrowIfOutOfRange(value, min, max, inclusive);

}
