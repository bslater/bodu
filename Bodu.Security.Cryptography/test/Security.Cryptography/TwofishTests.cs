// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="Twofish" /> algorithm class against the shared <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}" />
/// suite, verifying constructor defaults, key and IV generation, encryptor and decryptor creation, and disposal behaviour.
/// </summary>
[TestClass]
public sealed partial class TwofishTests
    : SymmetricAlgorithmTests<TwofishTests, Twofish>
{
    /// <inheritdoc />
    protected override Twofish CreateAlgorithm() => Twofish.Create();

    /// <inheritdoc />
    protected override void SetBlockMode(Twofish algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override CipherModeKind GetBlockMode(Twofish algorithm) =>
        algorithm.BlockMode;

    /// <inheritdoc />
    protected override SymmetricAlgorithmSpecification GetSpecification() =>
        new SymmetricAlgorithmSpecification
        {
            BlockSizeBits = 128,
            DefaultKeySizeBits = 256,
            LegalKeySizesBits = [128, 192, 256],
        };
}
