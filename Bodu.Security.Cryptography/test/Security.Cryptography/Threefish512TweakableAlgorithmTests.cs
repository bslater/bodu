// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish512TweakableAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Threefish512" /> — validating tweak property behaviour, defensive copies, invalid-size handling,
/// disposal semantics, and the curated <see cref="Threefish512KnownAnswers" /> data set at the algorithm tier.
/// </summary>
[TestClass]
public sealed partial class Threefish512TweakableAlgorithmTests
    : TweakableSymmetricAlgorithmTests<Threefish512TweakableAlgorithmTests, Threefish512>
{
    /// <inheritdoc />
    protected override Threefish512 CreateAlgorithm() => Threefish512.Create();

    /// <inheritdoc />
    protected override void SetBlockMode(Threefish512 algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override TweakableSymmetricAlgorithmSpecification GetSpecification() =>
        new TweakableSymmetricAlgorithmSpecification
        {
            BlockSizeBits = 512,
            DefaultKeySizeBits = 512,
            LegalKeySizesBits = [512],
            DefaultTweakSizeBits = 128,
            LegalTweakSizesBits = [128],
        };
}
