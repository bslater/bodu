// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{

    /// <summary>
    /// Verifies that the <see cref="BlockNonCryptographicHashAlgorithm{T}" /> constructor throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> equal to <c>blockSize</c> when the
    /// supplied block size is negative.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBlockSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new NegativeBlockSizeHasher());
        Assert.AreEqual("blockSize", ex.ParamName);
    }
    /// <summary>
    /// Verifies that the <see cref="BlockNonCryptographicHashAlgorithm{T}" /> constructor throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> equal to <c>blockSize</c> when the
    /// supplied block size is zero.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBlockSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new InvalidBlockSizeHasher());
        Assert.AreEqual("blockSize", ex.ParamName);
    }

}
