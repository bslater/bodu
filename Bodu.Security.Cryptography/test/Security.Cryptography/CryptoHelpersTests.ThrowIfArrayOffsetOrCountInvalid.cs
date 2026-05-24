// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfArrayOffsetOrCountInvalid.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(Array, int, int, string, string, string)"/>
    /// does not throw when the offset and count describe a valid segment.
    /// </summary>
    [TestMethod]
    [DataRow(0, 8)]
    [DataRow(2, 4)]
    [DataRow(0, 0)]
    [DataRow(8, 0)]
    public void ThrowIfArrayOffsetOrCountInvalid_WhenSegmentIsValid_ShouldNotThrow(int offset, int count)
    {
        var array = new byte[8];
        CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(Array, int, int, string, string, string)"/>
    /// throws an <see cref="ArgumentNullException"/> when the array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayOffsetOrCountInvalid_WhenArrayIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(Array, int, int, string, string, string)"/>
    /// throws an <see cref="ArgumentOutOfRangeException"/> when the offset is negative or exceeds the array length.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(9, 0)]
    public void ThrowIfArrayOffsetOrCountInvalid_WhenOffsetIsOutOfRange_ShouldThrowExactly(int offset, int count)
    {
        var array = new byte[8];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(Array, int, int, string, string, string)"/>
    /// throws an <see cref="ArgumentException"/> when the count is negative or exceeds the array length.
    /// </summary>
    [TestMethod]
    [DataRow(0, -1)]
    [DataRow(0, 9)]
    public void ThrowIfArrayOffsetOrCountInvalid_WhenCountIsInvalid_ShouldThrowExactly(int offset, int count)
    {
        var array = new byte[8];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(Array, int, int, string, string, string)"/>
    /// throws an <see cref="ArgumentException"/> when offset + count exceeds the array length.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayOffsetOrCountInvalid_WhenSegmentExceedsArrayLength_ShouldThrowExactly()
    {
        var array = new byte[8];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(array, 4, 5);
        });
    }
}
