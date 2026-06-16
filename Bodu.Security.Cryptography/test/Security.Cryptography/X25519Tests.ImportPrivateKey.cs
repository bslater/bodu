// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.ImportPrivateKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains X25519-specific tests for <see cref="X25519.ImportPrivateKey" />; the round-trip and length-validation
/// contract is inherited from the asymmetric base.
/// </summary>
public sealed partial class X25519Tests
{
    /// <summary>
    /// Verifies that <see cref="X25519.ImportPrivateKey" /> preserves the imported scalar byte-for-byte rather than
    /// storing a clamped form.
    /// </summary>
    [TestMethod]
    public void ImportPrivateKey_WhenScalarIsUnclamped_ShouldRoundTripExactBytes()
    {
        // Bits 0-2 set and bit 254 clear: clamping would alter this value, so an exact round-trip proves the
        // stored form is the caller's original scalar.
        byte[] privateKey = new byte[X25519.KeySizeInBytes];
        privateKey[0] = 0x07;
        privateKey[31] = 0x00;
        privateKey[15] = 0xAB;

        using var algorithm = new X25519();
        algorithm.ImportPrivateKey(privateKey);

        CollectionAssert.AreEqual(privateKey, algorithm.ExportPrivateKey());
    }
}
