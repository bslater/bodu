// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="Blowfish" /> algorithm class against the shared <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}" />
/// suite, verifying constructor defaults, key and IV generation, encryptor and decryptor creation, and disposal behaviour.
/// </summary>
[TestClass]
public sealed partial class BlowfishTests
    : SymmetricAlgorithmTests<BlowfishTests, Blowfish>
{
    /// <inheritdoc />
    protected override void SetBlockMode(Blowfish algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override CipherModeKind GetBlockMode(Blowfish algorithm) =>
        algorithm.BlockMode;

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            BlockSizeBits = 64,
            DefaultKeySizeBits = 128,
            LegalKeySizesBits = Enumerable.Range(4, 53)
                .Select(i => i * 8)
                .ToArray(),
        };
}
