// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStringTooLong.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringTooLong" /> throws <see cref="ArgumentNullException" />
    /// when the string value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfStringTooLong_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfStringTooLong(null!, 10);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringTooLong" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the string length exceeds the maximum.
    /// </summary>
    [TestMethod]
    [DataRow("abcdef", 5)]   // length 6 > max 5
    [DataRow("ab", 1)]       // length 2 > max 1
    [DataRow("x", 0)]        // length 1 > max 0
    public void ThrowIfStringTooLong_WhenLengthExceedsMaximum_ShouldThrowArgumentOutOfRangeException(string value, int maxLength)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfStringTooLong(value, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringTooLong" /> does not throw when the string length
    /// equals the maximum (boundary condition).
    /// </summary>
    [TestMethod]
    [DataRow("abcde", 5)]  // exactly at max
    [DataRow("", 0)]        // empty string at max 0
    public void ThrowIfStringTooLong_WhenLengthEqualsMaximum_ShouldNotThrow(string value, int maxLength) => ThrowHelper.ThrowIfStringTooLong(value, maxLength);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringTooLong" /> does not throw when the string length
    /// is strictly less than the maximum.
    /// </summary>
    [TestMethod]
    [DataRow("abc", 10)]
    [DataRow("", 1)]
    [DataRow("hello world", 100)]
    public void ThrowIfStringTooLong_WhenLengthIsBelowMaximum_ShouldNotThrow(string value, int maxLength) => ThrowHelper.ThrowIfStringTooLong(value, maxLength);
}
