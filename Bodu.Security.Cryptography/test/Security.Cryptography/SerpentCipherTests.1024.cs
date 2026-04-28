// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests.1024.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising the wide-block tweakable <see cref="Serpent1024Cipher" /> engine.
/// </summary>
/// <remarks>
/// Serpent-1024 is a non-standard construction with no published reference vectors. End-to-end correctness is covered by the
/// inherited round-trip tests in <see cref="BlockCipherTests{TTest, TCipher, TVariant}" />.
/// </remarks>
[TestClass]
internal partial class Serpent1024CipherTests
    : SerpentCipherTests<Serpent1024CipherTests, Serpent1024Cipher>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(SerpentCipherTestVariant variant) =>
        variant switch
        {
            SerpentCipherTestVariant.ZeroedKeyAndTweak => new()
            {
                BlockSize = 128,
                KeySize = 128,
                TweakSize = 16,
                TestKey = new byte[128],
                TestTweak = new byte[16],
            },
            SerpentCipherTestVariant.DefaultKeyAndTweak => new()
            {
                BlockSize = 128,
                KeySize = 128,
                TweakSize = 16,
                TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0x10, 128),
                TestTweak = CryptoTestUtilities.CreateIncrementalByteSequence(0, 16),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override SymmetricAlgorithm CreateInitialisedAlgorithm()
    {
        var algorithm = Serpent1024.Create();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        algorithm.GenerateTweak();
        return algorithm;
    }

    /// <inheritdoc />
    protected override Serpent1024Cipher CreateBlockCipher(SerpentCipherTestVariant variant)
    {
        var specification = GetSpecification(variant);
        return new Serpent1024Cipher(specification.TestKey, specification.TestTweak);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Serpent-1024 is a non-standard construction with no externally vetted reference vectors. The single self-referential
    /// vector below captures the cipher's own output at test-discovery time so that <see cref="EncryptTestData" /> and
    /// <see cref="DecryptTestData" /> have at least one row (MSTest fails empty <c>[DynamicData]</c> sources). Algorithmic
    /// correctness is anchored by the inherited round-trip and determinism tests.
    /// </remarks>
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(SerpentCipherTestVariant variant)
    {
        if (variant == SerpentCipherTestVariant.DefaultKeyAndTweak)
            yield break;

        var spec = GetSpecification(variant);
        byte[] input = new byte[spec.BlockSize];
        byte[] expected = new byte[spec.BlockSize];

        using (var cipher = new Serpent1024Cipher(spec.TestKey, spec.TestTweak))
            cipher.Encrypt(input, expected);

        yield return new KnownAnswerTest
        {
            Name           = "Serpent-1024 / ZeroedKeyAndTweak / plaintext=00×128 (self-referential regression vector)",
            Input          = input,
            ExpectedOutput = expected,
            CipherFactory  = () => new Serpent1024Cipher(spec.TestKey, spec.TestTweak),
        };
    }
}
