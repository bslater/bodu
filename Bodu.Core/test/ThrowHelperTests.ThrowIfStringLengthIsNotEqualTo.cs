// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfStringLengthIsNotEqualTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthIsNotEqualTo" /> throws
    /// <see cref="ArgumentNullException" /> when the string value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfStringLengthIsNotEqualTo_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthIsNotEqualTo(null!, 5);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthIsNotEqualTo" /> throws
    /// <see cref="ArgumentException" /> when the string length does not equal the expected length.
    /// </summary>
    [TestMethod]
    [DataRow("abc", 5)]    // shorter than expected
    [DataRow("abcdef", 5)] // longer than expected
    [DataRow("", 1)]       // empty but 1 expected
    [DataRow("ab", 0)]     // non-empty but 0 expected
    public void ThrowIfStringLengthIsNotEqualTo_WhenLengthDiffers_ShouldThrowArgumentException(string value, int expectedLength)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfStringLengthIsNotEqualTo(value, expectedLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfStringLengthIsNotEqualTo" /> does not throw when the
    /// string length exactly matches the expected length.
    /// </summary>
    [TestMethod]
    [DataRow("abcde", 5)]
    [DataRow("", 0)]
    [DataRow("x", 1)]
    [DataRow("US", 2)]          // typical country code length
    [DataRow("AAPLUSS00000", 12)] // ISIN-length string
    public void ThrowIfStringLengthIsNotEqualTo_WhenLengthMatches_ShouldNotThrow(string value, int expectedLength) => ThrowHelper.ThrowIfStringLengthIsNotEqualTo(value, expectedLength);
}
