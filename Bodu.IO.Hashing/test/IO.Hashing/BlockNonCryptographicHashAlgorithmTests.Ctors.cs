// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    public void Ctor_WhenBlockSizeIsNegative_ShouldThrowExactly()
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
    public void Ctor_WhenBlockSizeIsZero_ShouldThrowExactly()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new InvalidBlockSizeHasher());
        Assert.AreEqual("blockSize", ex.ParamName);
    }

}
