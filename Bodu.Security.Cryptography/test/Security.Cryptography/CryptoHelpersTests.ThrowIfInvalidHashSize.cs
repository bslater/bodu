// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfInvalidHashSize.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidHashSize(int, int[], string)"/> does not throw when
    /// the hash size is present in the permitted list.
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(256)]
    [DataRow(512)]
    public void ThrowIfInvalidHashSize_WhenSizeIsPermitted_ShouldNotThrow(int size) => CryptoHelpers.ThrowIfInvalidHashSize(size, [128, 256, 512]);

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidHashSize(int, int[], string)"/> throws an
    /// <see cref="ArgumentOutOfRangeException"/> when the hash size is not present in the permitted list.
    /// </summary>
    [TestMethod]
    [DataRow(64)]
    [DataRow(200)]
    [DataRow(1024)]
    public void ThrowIfInvalidHashSize_WhenSizeIsNotPermitted_ShouldThrowExactly(int size)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidHashSize(size, [128, 256, 512]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidHashSize(int, int[], string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the permitted-sizes array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidHashSize_WhenPermittedListIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidHashSize(256, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidHashSize(int, int[], string)"/> includes both the
    /// rejected size and the list of valid sizes in the exception message.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidHashSize_WhenSizeIsRejected_ShouldIncludeSizeAndValidSizesInMessage()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidHashSize(64, [128, 256]);
        });

        Assert.IsTrue(ex.Message.Contains("64", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("128", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("256", StringComparison.Ordinal));
    }
}
