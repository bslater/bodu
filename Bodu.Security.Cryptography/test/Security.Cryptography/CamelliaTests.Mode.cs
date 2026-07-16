// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaTests.Mode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class CamelliaTests
{
    /// <summary>
    /// Verifies that assigning the inherited <see cref="SymmetricAlgorithm.Mode" /> synchronizes
    /// <see cref="Camellia.BlockMode" />, so the mode actually used by encryptor / decryptor creation matches the
    /// assigned value rather than silently remaining the default.
    /// </summary>
    /// <param name="mode">The inherited cipher mode to assign.</param>
    /// <param name="expected">The <see cref="CipherModeKind" /> the assignment should produce.</param>
    [TestMethod]
    [DataRow(CipherMode.ECB, CipherModeKind.ECB)]
    [DataRow(CipherMode.CBC, CipherModeKind.CBC)]
    [DataRow(CipherMode.CFB, CipherModeKind.CFB)]
    public void Mode_WhenSet_ShouldSynchronizeBlockMode(CipherMode mode, CipherModeKind expected)
    {
        using var algorithm = new Camellia();

        algorithm.Mode = mode;

        Assert.AreEqual(expected, algorithm.BlockMode);
    }

    /// <summary>
    /// Verifies that assigning <see cref="Camellia.BlockMode" /> to a value with a standard <see cref="CipherMode" />
    /// equivalent synchronizes the inherited <see cref="SymmetricAlgorithm.Mode" />.
    /// </summary>
    /// <param name="blockMode">The extended block mode to assign.</param>
    /// <param name="expected">The inherited <see cref="CipherMode" /> the assignment should produce.</param>
    [TestMethod]
    [DataRow(CipherModeKind.ECB, CipherMode.ECB)]
    [DataRow(CipherModeKind.CBC, CipherMode.CBC)]
    [DataRow(CipherModeKind.CFB, CipherMode.CFB)]
    [DataRow(CipherModeKind.OFB, CipherMode.OFB)]
    public void BlockMode_WhenSetToMappableValue_ShouldSynchronizeMode(CipherModeKind blockMode, CipherMode expected)
    {
        using var algorithm = new Camellia();

        algorithm.BlockMode = blockMode;

        Assert.AreEqual(expected, algorithm.Mode);
    }
}
