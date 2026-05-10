// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStringLengthOutOfRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> throws
    /// <see cref="ArgumentNullException" /> when the string value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfStringLengthOutOfRange_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthOutOfRange(null!, 2, 10);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the string length is below the minimum.
    /// </summary>
    [TestMethod]
    [DataRow("a", 2, 10)]   // length 1 < min 2
    [DataRow("", 1, 5)]     // length 0 < min 1
    [DataRow("abc", 5, 10)] // length 3 < min 5
    public void ThrowIfStringLengthOutOfRange_WhenLengthIsBelowMinimum_ShouldThrowArgumentOutOfRangeException(string value, int minLength, int maxLength)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthOutOfRange(value, minLength, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the string length exceeds the maximum.
    /// </summary>
    [TestMethod]
    [DataRow("abcdef", 1, 5)]   // length 6 > max 5
    [DataRow("hello!", 2, 5)]   // length 6 > max 5
    [DataRow("abcde", 1, 4)]    // length 5 > max 4
    public void ThrowIfStringLengthOutOfRange_WhenLengthExceedsMaximum_ShouldThrowArgumentOutOfRangeException(string value, int minLength, int maxLength)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthOutOfRange(value, minLength, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthOutOfRange" /> does not throw when the string
    /// length is within the specified range (inclusive on both boundaries).
    /// </summary>
    [TestMethod]
    [DataRow("ab", 2, 10)]      // exactly at min
    [DataRow("abcdefghij", 2, 10)] // exactly at max
    [DataRow("abcde", 2, 10)]   // within range
    [DataRow("", 0, 5)]         // min = 0, empty string valid
    [DataRow("x", 1, 1)]        // min == max == length
    public void ThrowIfStringLengthOutOfRange_WhenLengthIsWithinRange_ShouldNotThrow(string value, int minLength, int maxLength)
    {
        ThrowHelper.ThrowIfStringLengthOutOfRange(value, minLength, maxLength);
    }
}
