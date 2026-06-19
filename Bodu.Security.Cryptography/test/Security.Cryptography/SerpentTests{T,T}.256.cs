// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentTests{T,T}.256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Serpent256" /> — validating tweak property behaviour, defensive copies, invalid-size handling, and disposal
/// semantics for the 256-bit wide-block Serpent variant.
/// </summary>
[TestClass]
public partial class Serpent256Tests
    : SerpentTests<Serpent256Tests, Serpent256>
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
