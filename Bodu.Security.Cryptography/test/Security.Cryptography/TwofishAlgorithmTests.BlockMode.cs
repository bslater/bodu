// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishAlgorithmTests.BlockMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class TwofishAlgorithmTests
{
    /// <summary>
    /// Verifies that <see cref="Twofish.BlockMode" /> defaults to <see cref="CipherBlockMode.CBC" /> when the
    /// algorithm is first created.
    /// </summary>
    [TestMethod]
    public void BlockMode_WhenDefault_ShouldBeCbc()
    {
        using Twofish algorithm = CreateAlgorithm();
        Assert.AreEqual(CipherBlockMode.CBC, algorithm.BlockMode);
    }

    /// <summary>
    /// Verifies that setting <see cref="Twofish.BlockMode" /> persists and is returned by the next get.
    /// </summary>
    [TestMethod]
    [DataRow(CipherBlockMode.ECB)]
    [DataRow(CipherBlockMode.CBC)]
    [DataRow(CipherBlockMode.CFB)]
    [DataRow(CipherBlockMode.OFB)]
    [DataRow(CipherBlockMode.CTR)]
    public void BlockMode_WhenSet_ShouldReturnSameValueOnGet(CipherBlockMode mode)
    {
        using Twofish algorithm = CreateAlgorithm();
        algorithm.BlockMode = mode;
        Assert.AreEqual(mode, algorithm.BlockMode);
    }

    /// <summary>
    /// Verifies that setting <see cref="Twofish.BlockMode" /> to a value that maps to a standard
    /// <see cref="CipherMode" /> (ECB, CBC, CFB, OFB) also updates <see cref="SymmetricAlgorithm.Mode" />.
    /// </summary>
    [TestMethod]
    [DataRow(CipherBlockMode.ECB, CipherMode.ECB)]
    [DataRow(CipherBlockMode.CBC, CipherMode.CBC)]
    [DataRow(CipherBlockMode.CFB, CipherMode.CFB)]
    [DataRow(CipherBlockMode.OFB, CipherMode.OFB)]
    public void BlockMode_WhenSetToMappableValue_ShouldSynchronizeModeProperty(CipherBlockMode blockMode, CipherMode expectedMode)
    {
        using Twofish algorithm = CreateAlgorithm();
        algorithm.BlockMode = blockMode;
        Assert.AreEqual(expectedMode, algorithm.Mode);
    }

    /// <summary>
    /// Verifies that setting <see cref="Twofish.BlockMode" /> to a value with no <see cref="CipherMode" />
    /// equivalent (CTR, XTS, OCB, EAX, SIV) does not update <see cref="SymmetricAlgorithm.Mode" />.
    /// </summary>
    [TestMethod]
    [DataRow(CipherBlockMode.CTR)]
    [DataRow(CipherBlockMode.XTS)]
    [DataRow(CipherBlockMode.OCB)]
    [DataRow(CipherBlockMode.EAX)]
    [DataRow(CipherBlockMode.SIV)]
    public void BlockMode_WhenSetToUnmappableValue_ShouldNotChangeModeProperty(CipherBlockMode mode)
    {
        using Twofish algorithm = CreateAlgorithm();
        CipherMode modeBefore = algorithm.Mode;
        algorithm.BlockMode = mode;
        Assert.AreEqual(modeBefore, algorithm.Mode);
    }
}
