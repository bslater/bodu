// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="XtsModeTransform" /> (IEEE Std 1619-2007 / NIST SP 800-38E).
/// </summary>
[TestClass]
public sealed partial class XtsModeTransformTests : BlockCipherModeTests<XtsModeTransform>
{
    // XTS requires 16-byte blocks (AES) and uses a tweak (sector number) instead of an IV.
    protected override int ExpectedBlockSize => 16;
    protected override string IvParameterName => "tweak";
    protected override bool UsesInitializationVector => true;
    protected override bool UsesChaining => false; // XEX: no inter-block state chain
    protected override bool RequiresBlockAlignedInput => true;

    /// <summary>
    /// Creates an <see cref="XtsModeTransform" /> with <paramref name="cipher" /> as the data
    /// cipher (Key₁) and a separate <see cref="MonitoringBlockCipher" /> keyed differently
    /// (xorMask 0x55) as the tweak cipher (Key₂). The <paramref name="iv" /> is used as the
    /// sector number / tweak.
    /// </summary>
    /// <remarks>
    /// Structural tests (chaining, round-trip) work because both encrypt and decrypt use the same
    /// pair of ciphers. KAT tests instantiate XtsModeTransform directly with two real AES ciphers.
    /// </remarks>
    protected override XtsModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new XtsModeTransform(
            dataCipher: cipher,
            tweakCipher: new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55),
            tweak: iv);
}
