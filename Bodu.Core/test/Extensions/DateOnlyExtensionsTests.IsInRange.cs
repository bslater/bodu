// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsInRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsInRange(DateTime, DateTime, DateTime)" /> correctly evaluates date range inclusion.
    /// </summary>
    /// <param name="inputDateTime">The date to evaluate.</param>
    /// <param name="startDateTime">The start of the range.</param>
    /// <param name="endDateTime">The end of the range.</param>
    /// <param name="expected">The expected actual.</param>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.GetIsInRangeTestCases), typeof(DateTimeExtensionsTests))]
    public void IsInRange_ForDateTime_ShouldReturnExpectedResult(DateTime inputDateTime, DateTime startDateTime, DateTime endDateTime, bool expected)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var start = DateOnly.FromDateTime(startDateTime);
        var end = DateOnly.FromDateTime(endDateTime);
        var actual = input.IsInRange(start, end);

        Assert.AreEqual(expected, actual, $"Failed for inputDateTime={inputDateTime:yyyy-MM-dd}, start={start:yyyy-MM-dd}, end={end:yyyy-MM-dd}");
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsInRange(DateTime?, DateTime, DateTime)" /> correctly evaluates nullable date range inclusion.
    /// </summary>
    /// <param name="inputDateTime">The nullable date to evaluate.</param>
    /// <param name="startDateTime">The start of the range.</param>
    /// <param name="endDateTime">The end of the range.</param>
    /// <param name="expected">The expected actual.</param>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.GetIsInRangeNullableTestCases), typeof(DateTimeExtensionsTests))]
    public void IsInRange_ForNullableDateTime_ShouldReturnExpectedResult(DateTime? inputDateTime, DateTime startDateTime, DateTime endDateTime, bool expected)
    {
        DateOnly? input = inputDateTime.HasValue ? DateOnly.FromDateTime(inputDateTime.Value) : null;
        var start = DateOnly.FromDateTime(startDateTime);
        var end = DateOnly.FromDateTime(endDateTime);
        var actual = input.IsInRange(start, end);

        Assert.AreEqual(expected, actual, $"Failed for inputDateTime={(inputDateTime.HasValue ? inputDateTime.Value.ToString("yyyy-MM-dd") : "null")}, start={start:yyyy-MM-dd}, end={end:yyyy-MM-dd}");
    }

}
