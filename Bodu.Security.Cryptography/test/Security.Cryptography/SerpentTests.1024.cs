// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentTests.1024.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> base test suite against
/// <see cref="Serpent1024" /> — validating tweak property behaviour, defensive copies, invalid-size handling, and disposal
/// semantics for the 1024-bit wide-block Serpent variant.
/// </summary>
[TestClass]
public partial class Serpent1024Tests
    : SerpentTests<Serpent1024Tests, Serpent1024>
{
    /// <inheritdoc />
    protected override Serpent1024 CreateAlgorithm() => new Serpent1024();

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
