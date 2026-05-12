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
    /// Verifies that <see cref="Twofish.BlockMode" /> defaults to <see cref="CipherModeKind.CBC" /> when the
    /// algorithm is first created.
    /// </summary>
    [TestMethod]
    public void BlockMode_WhenDefault_ShouldBeCbc()
    {
        using Twofish algorithm = CreateAlgorithm();
        Assert.AreEqual(CipherModeKind.CBC, algorithm.BlockMode);
    }

    /// <summary>
    /// Verifies that setting <see cref="Twofish.BlockMode" /> persists and is returned by the next get.
    /// </summary>
    [TestMethod]
    [DataRow(CipherModeKind.ECB)]
    [DataRow(CipherModeKind.CBC)]
    [DataRow(CipherModeKind.CFB)]
    [DataRow(CipherModeKind.OFB)]
    [DataRow(CipherModeKind.CTR)]
    public void BlockMode_WhenSet_ShouldReturnSameValueOnGet(CipherModeKind mode)
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
    [DataRow(CipherModeKind.ECB, CipherMode.ECB)]
    [DataRow(CipherModeKind.CBC, CipherMode.CBC)]
    [DataRow(CipherModeKind.CFB, CipherMode.CFB)]
    [DataRow(CipherModeKind.OFB, CipherMode.OFB)]
    public void BlockMode_WhenSetToMappableValue_ShouldSynchronizeModeProperty(CipherModeKind blockMode, CipherMode expectedMode)
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
    [DataRow(CipherModeKind.CTR)]
    [DataRow(CipherModeKind.XTS)]
    [DataRow(CipherModeKind.OCB)]
    [DataRow(CipherModeKind.EAX)]
    [DataRow(CipherModeKind.SIV)]
    public void BlockMode_WhenSetToUnmappableValue_ShouldNotChangeModeProperty(CipherModeKind mode)
    {
        using Twofish algorithm = CreateAlgorithm();
        CipherMode modeBefore = algorithm.Mode;
        algorithm.BlockMode = mode;
        Assert.AreEqual(modeBefore, algorithm.Mode);
    }
}
