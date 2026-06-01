// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTests.256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    : ThreefishTests<Threefish256Tests, Threefish256>
{
    /// <inheritdoc />
    protected override TweakableSymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            BlockSizeBits = 256,
            DefaultKeySizeBits = 256,
            LegalKeySizesBits = [256],
            DefaultTweakSizeBits = 128,
            LegalTweakSizesBits = [128],
        };
}
