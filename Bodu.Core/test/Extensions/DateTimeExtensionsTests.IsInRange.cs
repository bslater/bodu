// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsInRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    private static readonly DateTime s_sampleDate = new(2024, 4, 15); // 15-Apr-2024

    /// <summary>
    /// Provides test cases for verifying <see cref="DateTimeExtensions.IsInRange(DateTime?, DateTime, DateTime)" />.
    /// </summary>
    public static IEnumerable<object[]> GetIsInRangeNullableTestCases()
    {
        foreach (var testCase in GetIsInRangeTestCases())
            yield return new object[] { (DateTime?)testCase[0], testCase[1], testCase[2], testCase[3] };

        // additional nullable-specific test case
        yield return new object[] { null, s_sampleDate.AddDays(-10), s_sampleDate.AddDays(10), false }; // Null input
    }

    /// <summary>
    /// Provides test cases for verifying <see cref="DateTimeExtensions.IsInRange(DateTime, DateTime, DateTime)" />.
    /// </summary>
    public static IEnumerable<object[]> GetIsInRangeTestCases()
    {
        yield return new object[] { s_sampleDate, s_sampleDate.AddDays(-1), s_sampleDate.AddDays(1), true };    // Inside range
        yield return new object[] { s_sampleDate, s_sampleDate, s_sampleDate.AddDays(2), true };                // Equal to start
        yield return new object[] { s_sampleDate, s_sampleDate.AddDays(-2), s_sampleDate, true };               // Equal to end
        yield return new object[] { s_sampleDate, s_sampleDate.AddDays(1), s_sampleDate.AddDays(5), false };    // Before range
        yield return new object[] { s_sampleDate, s_sampleDate.AddDays(-5), s_sampleDate.AddDays(-1), false };  // After range
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsInRange(DateTime, DateTime, DateTime)" /> correctly evaluates date range inclusion.
    /// </summary>
    /// <param name="value">The date to evaluate.</param>
    /// <param name="start">The start of the range.</param>
    /// <param name="end">The end of the range.</param>
    /// <param name="expected">The expected actual.</param>
    [TestMethod]
    [DynamicData(nameof(GetIsInRangeTestCases))]
    public void IsInRange_ForDateTime_ShouldReturnExpectedResult(DateTime value, DateTime start, DateTime end, bool expected)
    {
        var actual = value.IsInRange(start, end);

        Assert.AreEqual(expected, actual, $"Failed for value={value:yyyy-MM-dd}, start={start:yyyy-MM-dd}, end={end:yyyy-MM-dd}");
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsInRange(DateTime?, DateTime, DateTime)" /> correctly evaluates nullable date range inclusion.
    /// </summary>
    /// <param name="value">The nullable date to evaluate.</param>
    /// <param name="start">The start of the range.</param>
    /// <param name="end">The end of the range.</param>
    /// <param name="expected">The expected actual.</param>
    [TestMethod]
    [DynamicData(nameof(GetIsInRangeNullableTestCases))]
    public void IsInRange_ForNullableDateTime_ShouldReturnExpectedResult(DateTime? value, DateTime start, DateTime end, bool expected)
    {
        var actual = value.IsInRange(start, end);

        Assert.AreEqual(expected, actual, $"Failed for value={(value.HasValue ? value.Value.ToString("yyyy-MM-dd") : "null")}, start={start:yyyy-MM-dd}, end={end:yyyy-MM-dd}");
    }

}
