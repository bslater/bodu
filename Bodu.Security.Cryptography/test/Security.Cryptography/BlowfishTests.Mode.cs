// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTests.Mode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class BlowfishTests
{
    /// <summary>
    /// Verifies that assigning the inherited <see cref="SymmetricAlgorithm.Mode" /> synchronizes
    /// <see cref="Blowfish.BlockMode" />, so the mode actually used by encryptor / decryptor creation matches the
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
        using var algorithm = new Blowfish();

        algorithm.Mode = mode;

        Assert.AreEqual(expected, algorithm.BlockMode);
    }
}
