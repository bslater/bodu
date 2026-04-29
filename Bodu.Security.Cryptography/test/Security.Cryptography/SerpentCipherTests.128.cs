// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests.128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising <see cref="Serpent128Cipher" /> against the canonical Serpent test vectors published by the
/// Bouncy Castle project (derived from the Anderson/Biham/Knudsen AES submission reference implementation).
/// </summary>
[TestClass]
internal partial class Serpent128CipherTests
    : SerpentCipherTests<Serpent128CipherTests, Serpent128Cipher>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(TweakableBlockCipherVariant variant) =>
        variant switch
        {
            TweakableBlockCipherVariant.ZeroedKeyAndTweak => new()
            {
                BlockSize = 16,
                KeySize = 16,
                TweakSize = 0,
                TestKey = new byte[16],
                TestTweak = null,
            },
            TweakableBlockCipherVariant.DefaultKeyAndTweak => new()
            {
                BlockSize = 16,
                KeySize = 16,
                TweakSize = 0,
                TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0x00, 16),
                TestTweak = null,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override SymmetricAlgorithm CreateInitialisedAlgorithm()
    {
        var algorithm = Serpent128.Create();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return algorithm;
    }

    /// <inheritdoc />
    protected override Serpent128Cipher CreateBlockCipher(TweakableBlockCipherVariant variant)
    {
        var specification = GetSpecification(variant);
        return new Serpent128Cipher(specification.TestKey);
    }

    /// <inheritdoc />
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(TweakableBlockCipherVariant variant) =>
        AdaptKnownAnswers(
            Serpent128KnownAnswers.For(variant),
            answer => new Serpent128Cipher(answer.Key!));
}
