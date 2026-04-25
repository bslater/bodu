// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishTests.Defaults.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class TwofishTests
{
    /// <summary>
    /// Verifies that a newly constructed <see cref="Twofish" /> instance defaults to a 256-bit key size.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenDefault_ShouldBe256()
    {
        using var algorithm = CreateAlgorithm();
        Assert.AreEqual(256, algorithm.KeySize);
    }

    /// <summary>
    /// Verifies that a newly constructed <see cref="Twofish" /> instance defaults to a 128-bit block size.
    /// </summary>
    [TestMethod]
    public void BlockSize_WhenDefault_ShouldBe128()
    {
        using var algorithm = CreateAlgorithm();
        Assert.AreEqual(128, algorithm.BlockSize);
    }

    /// <summary>
    /// Verifies that a newly constructed <see cref="Twofish" /> instance defaults to
    /// <see cref="PaddingMode.PKCS7" /> padding.
    /// </summary>
    [TestMethod]
    public void Padding_WhenDefault_ShouldBePkcs7()
    {
        using var algorithm = CreateAlgorithm();
        Assert.AreEqual(PaddingMode.PKCS7, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that a newly constructed <see cref="Twofish" /> instance defaults to
    /// <see cref="CipherMode.CBC" /> mode.
    /// </summary>
    [TestMethod]
    public void Mode_WhenDefault_ShouldBeCbc()
    {
        using var algorithm = CreateAlgorithm();
        Assert.AreEqual(CipherMode.CBC, algorithm.Mode);
    }
}
