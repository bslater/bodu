// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentTests{T,T}.512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Serpent512" /> — validating tweak property behaviour, defensive copies, invalid-size handling, and disposal
/// semantics for the 512-bit wide-block Serpent variant.
/// </summary>
[TestClass]
public partial class Serpent512Tests
    : SerpentTests<Serpent512Tests, Serpent512>
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
