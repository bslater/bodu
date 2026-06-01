// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTests.512.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Threefish512" /> — validating tweak property behaviour, defensive copies, invalid-size handling,
/// disposal semantics, and the curated <see cref="Threefish512CipherTests" /> data set at the algorithm tier.
/// </summary>
[TestClass]
public sealed partial class Threefish512Tests
    : ThreefishTests<Threefish512Tests, Threefish512>
{
    /// <inheritdoc />
    protected override TweakableSymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            BlockSizeBits = 512,
            DefaultKeySizeBits = 512,
            LegalKeySizesBits = [512],
            DefaultTweakSizeBits = 128,
            LegalTweakSizesBits = [128],
        };
}
