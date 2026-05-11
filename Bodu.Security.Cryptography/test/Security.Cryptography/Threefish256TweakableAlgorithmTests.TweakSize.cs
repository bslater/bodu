// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256TweakableAlgorithmTests.TweakSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class Threefish256TweakableAlgorithmTests
{
    /// <summary>
    /// Verifies that assigning <see cref="TweakableSymmetricAlgorithm.TweakSize" /> to <c>0</c>
    /// throws <see cref="CryptographicException" /> rather than leaving the algorithm in an
    /// invalid configuration where <see cref="TweakableSymmetricAlgorithm.Tweak" /> would later
    /// fail with <see cref="NullReferenceException" /> or another unexpected exception.
    /// </summary>
    [TestMethod]
    public void TweakSize_WhenSetToZero_ShouldThrowExactly()
    {
        using Threefish256 algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.TweakSize = 0;
        });
    }

    /// <summary>
    /// Verifies that assigning a wrongly-sized <see cref="TweakableSymmetricAlgorithm.TweakSize" />
    /// throws <see cref="CryptographicException" /> for a representative range of off-by-one and
    /// off-by-many values.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(64)]
    [DataRow(127)]
    [DataRow(129)]
    [DataRow(256)]
    [DataRow(-1)]
    public void TweakSize_WhenSetToInvalidValue_ShouldThrowExactly(int value)
    {
        using Threefish256 algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.TweakSize = value;
        });
    }
}
