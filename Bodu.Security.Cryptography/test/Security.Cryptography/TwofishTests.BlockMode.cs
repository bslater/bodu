// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishTests.BlockMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class TwofishTests
{
    /// <summary>
    /// Verifies that setting <see cref="Twofish.BlockMode" /> to a value that maps to a standard
    /// <see cref="CipherMode" /> (ECB, CBC, CFB, OFB) also updates <see cref="SymmetricAlgorithm.Mode" />.
    /// This Mode-synchronisation behaviour is Twofish-specific and is not part of the broader
    /// <c>BlockMode</c> contract shared by other Bodu ciphers (e.g. Serpent / Threefish use a plain
    /// auto-property and do not sync to <see cref="SymmetricAlgorithm.Mode" />).
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
    /// Counterpart to <see cref="BlockMode_WhenSetToMappableValue_ShouldSynchronizeModeProperty" />.
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

    /// <summary>
    /// Verifies that assigning the inherited <see cref="SymmetricAlgorithm.Mode" /> synchronizes
    /// <see cref="Twofish.BlockMode" />, so the mode actually used by encryptor / decryptor creation matches the
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
        using Twofish algorithm = CreateAlgorithm();
        algorithm.Mode = mode;
        Assert.AreEqual(expected, algorithm.BlockMode);
    }
}
