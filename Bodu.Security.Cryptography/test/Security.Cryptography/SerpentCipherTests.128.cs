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
    protected override BlockCipherSpecification GetSpecification(SerpentCipherTestVariant variant) =>
        variant switch
        {
            SerpentCipherTestVariant.ZeroedKeyAndTweak => new()
            {
                BlockSize = 16,
                KeySize = 16,
                TweakSize = 0,
                TestKey = new byte[16],
                TestTweak = null,
            },
            SerpentCipherTestVariant.DefaultKeyAndTweak => new()
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
    protected override Serpent128Cipher CreateBlockCipher(SerpentCipherTestVariant variant)
    {
        var specification = GetSpecification(variant);
        return new Serpent128Cipher(specification.TestKey);
    }

    /// <inheritdoc />
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(SerpentCipherTestVariant variant) =>
        variant switch
        {
            // Canonical Serpent-128 test vectors from the Bouncy Castle Serpent engine test suite
            // (bc-java: crypto/test/src/test/java/org/bouncycastle/crypto/test/SerpentTest.java).
            // These vectors match the standard Linux kernel / Bouncy Castle byte-ordering convention
            // (little-endian word packing, no external IP/FP permutation).
            SerpentCipherTestVariant.ZeroedKeyAndTweak =>
            [
                new KnownAnswerTest
                {
                    Name           = "Serpent-128 / key=00×16 / plaintext=00×16",
                    Input          = new byte[16],
                    ExpectedOutput = Convert.FromHexString("49672BA898D98DF95019180445491089"),
                    CipherFactory  = () => new Serpent128Cipher(new byte[16]),
                },
            ],
            SerpentCipherTestVariant.DefaultKeyAndTweak =>
            [
                new KnownAnswerTest
                {
                    Name           = "Serpent-128 / key=000102…0F / plaintext=00112233…EEFF (Bouncy Castle)",
                    Input          = Convert.FromHexString("00112233445566778899AABBCCDDEEFF"),
                    ExpectedOutput = Convert.FromHexString("563E2CF8740A27C164804560391E9B27"),
                    CipherFactory  = () => new Serpent128Cipher(Convert.FromHexString("000102030405060708090A0B0C0D0E0F")),
                },
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };
}
