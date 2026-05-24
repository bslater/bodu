// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentTransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests against the
/// wide-block tweakable <see cref="SerpentTransform" /> implementation, distinct from the standardised 128-bit
/// <see cref="Serpent128Transform" /> covered by <see cref="Serpent128TransformTests" />. KAT coverage for the wide-block
/// variants is provided at the block-cipher tier by <see cref="Serpent256CipherTests" /> /
/// <see cref="Serpent512CipherTests" /> / <see cref="Serpent1024CipherTests" />, whose rows are cross-validated against
/// <c>tools/cipher-vectors/wide_serpent.py</c>; this transform-tier test class therefore only exercises the behavioural
/// surface (construction, raw-block transform, disposal).
/// </summary>
[TestClass]
internal sealed class SerpentTransformTests
    : BlockCipherTransformTests<SerpentTransformTests, SerpentTransform>
{
    /// <inheritdoc />
    protected override SerpentTransform CreateAlgorithm()
    {
        var algorithm = new Serpent256();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        algorithm.GenerateTweak();
        return (SerpentTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV, algorithm.Tweak);
    }
}
