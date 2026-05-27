// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256Tests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Threefish256" /> — validating tweak property behaviour, defensive copies, invalid-size handling,
/// disposal semantics, and the curated <see cref="Threefish256CipherTests" /> data set at the algorithm tier.
/// </summary>
[TestClass]
public sealed partial class Threefish256Tests
    : TweakableSymmetricAlgorithmTests<Threefish256Tests, Threefish256>
{
    /// <inheritdoc />
    protected override Threefish256 CreateAlgorithm() => Threefish256.Create();

    /// <inheritdoc />
    protected override void SetBlockMode(Threefish256 algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override TweakableSymmetricAlgorithmSpecification GetSpecification() =>
        new TweakableSymmetricAlgorithmSpecification
        {
            BlockSizeBits = 256,
            DefaultKeySizeBits = 256,
            LegalKeySizesBits = [256],
            DefaultTweakSizeBits = 128,
            LegalTweakSizesBits = [128],
        };
}
