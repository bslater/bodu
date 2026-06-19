// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTests{T,T}.1024.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    : ThreefishTests<Threefish1024Tests, Threefish1024>
{
    /// <inheritdoc />
    protected override TweakableSymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            BlockSizeBits = 1024,
            DefaultKeySizeBits = 1024,
            LegalKeySizesBits = [1024],
            DefaultTweakSizeBits = 128,
            LegalTweakSizesBits = [128],
        };
}
