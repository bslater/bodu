// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish1024Tests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Threefish1024" /> — validating tweak property behaviour, defensive copies, invalid-size handling,
/// disposal semantics, and the curated <see cref="Threefish1024CipherTests" /> data set at the algorithm tier.
/// </summary>
[TestClass]
public sealed partial class Threefish1024Tests
    : TweakableSymmetricAlgorithmTests<Threefish1024Tests, Threefish1024>
{
    /// <inheritdoc />
    protected override Threefish1024 CreateAlgorithm() => Threefish1024.Create();

    /// <inheritdoc />
    protected override void SetBlockMode(Threefish1024 algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected override TweakableSymmetricAlgorithmSpecification GetSpecification() =>
        new TweakableSymmetricAlgorithmSpecification
        {
            BlockSizeBits = 1024,
            DefaultKeySizeBits = 1024,
            LegalKeySizesBits = [1024],
            DefaultTweakSizeBits = 128,
            LegalTweakSizesBits = [128],
        };
}
