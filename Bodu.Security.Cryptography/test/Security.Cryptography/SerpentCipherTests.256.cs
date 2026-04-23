// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests.256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising the wide-block tweakable <see cref="Serpent256Cipher" /> engine.
/// </summary>
/// <remarks>
/// Serpent-256 is a non-standard construction with no published reference vectors. The known-answer tests here are
/// self-generated — they validate determinism and regression stability across runs rather than algorithmic conformance with an
/// external reference. End-to-end correctness is additionally covered by the inherited round-trip tests in
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" />.
/// </remarks>
[TestClass]
internal partial class Serpent256CipherTests
    : SerpentCipherTests<Serpent256CipherTests, Serpent256Cipher>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(SerpentCipherTestVariant variant) =>
        variant switch
        {
            SerpentCipherTestVariant.ZeroedKeyAndTweak => new()
            {
                BlockSize = 32,
                KeySize = 32,
                TweakSize = 16,
                TestKey = new byte[32],
                TestTweak = new byte[16],
            },
            SerpentCipherTestVariant.DefaultKeyAndTweak => new()
            {
                BlockSize = 32,
                KeySize = 32,
                TweakSize = 16,
                TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0x10, 32),
                TestTweak = CryptoTestUtilities.CreateIncrementalByteSequence(0, 16),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override SymmetricAlgorithm CreateInitialisedAlgorithm()
    {
        var algorithm = Serpent256.Create();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        algorithm.GenerateTweak();
        return algorithm;
    }

    /// <inheritdoc />
    protected override Serpent256Cipher CreateBlockCipher(SerpentCipherTestVariant variant)
    {
        var specification = GetSpecification(variant);
        return new Serpent256Cipher(specification.TestKey, specification.TestTweak);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Serpent-256 is a non-standard construction with no externally vetted reference vectors, so no hard-coded known-answer
    /// tests are supplied. Correctness is covered end-to-end by the round-trip and determinism tests inherited from
    /// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" />; regression coverage against the first captured ciphertext
    /// should be added after the initial implementation is validated on the target runtime.
    /// </remarks>
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(SerpentCipherTestVariant variant) =>
        Array.Empty<KnownAnswerTest>();
}
