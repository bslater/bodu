// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.IsPrime.MoreCoverage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>IsPrime</c> correctly identifies <see cref="int.MaxValue"/> as a Mersenne prime
    /// (<c>M31 = 2^31 - 1</c>).
    /// </summary>
    [TestMethod]
    public void IsPrime_Int_WhenMaxValue_ShouldReturnTrue() =>
        Assert.IsTrue(int.MaxValue.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> correctly identifies <see cref="ulong.MaxValue"/> (<c>2^64 - 1</c>)
    /// as composite. The smallest prime factor is <c>3</c>, so the trial-division loop rejects quickly.
    /// </summary>
    [TestMethod]
    public void IsPrime_ULong_WhenMaxValue_ShouldReturnFalse() =>
        Assert.IsFalse(ulong.MaxValue.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> correctly identifies <see cref="ushort.MaxValue"/> as composite
    /// (<c>65535 = 3 * 5 * 17 * 257</c>).
    /// </summary>
    [TestMethod]
    public void IsPrime_UShort_WhenMaxValue_ShouldReturnFalse() =>
        Assert.IsFalse(ushort.MaxValue.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> correctly identifies <see cref="short.MaxValue"/> as a Mersenne prime
    /// (<c>M15 = 2^15 - 1 = 32767 = 7 * 31 * 151</c>) — actually a composite. Confirms the trial-division
    /// rejects it.
    /// </summary>
    [TestMethod]
    public void IsPrime_Short_WhenMaxValue_ShouldReturnFalse() =>
        Assert.IsFalse(short.MaxValue.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> rejects <c>short.MinValue</c> (a large negative composite-like input)
    /// without throwing.
    /// </summary>
    [TestMethod]
    public void IsPrime_Short_WhenMinValue_ShouldReturnFalse() =>
        Assert.IsFalse(short.MinValue.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> rejects <c>long.MinValue</c> without throwing.
    /// </summary>
    [TestMethod]
    public void IsPrime_Long_WhenMinValue_ShouldReturnFalse() =>
        Assert.IsFalse(long.MinValue.IsPrime());
}
