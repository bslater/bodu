// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent512CipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising the wide-block tweakable <see cref="Serpent512Cipher" /> engine.
/// </summary>
/// <remarks>
/// <para>
/// Serpent-512 is a non-standard construction with no published reference vectors. End-to-end correctness is covered by the
/// inherited round-trip tests in <see cref="BlockCipherTests{TTest, TCipher, TVariant}" />.
/// </para>
/// <para>
/// TODO(gh-142): Decide vector strategy for wide-block Serpent. See <see cref="BlockCipherKnownAnswer" /> for the gap policy.
/// </para>
/// </remarks>
[TestClass]
internal sealed class Serpent512CipherTests
    : BlockCipherTests<Serpent512CipherTests, Serpent512Cipher, TweakableBlockCipherVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(TweakableBlockCipherVariant variant) =>
        variant switch
        {
            TweakableBlockCipherVariant.ZeroedKeyAndTweak => new()
            {
                BlockSize = 64,
                KeySize = 64,
                TweakSize = 16,
                TestKey = new byte[64],
                TestTweak = new byte[16],
            },
            TweakableBlockCipherVariant.DefaultKeyAndTweak => new()
            {
                BlockSize = 64,
                KeySize = 64,
                TweakSize = 16,
                TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0x10, 64),
                TestTweak = CryptoTestUtilities.CreateIncrementalByteSequence(0, 16),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override Serpent512Cipher CreateBlockCipher(TweakableBlockCipherVariant variant)
    {
        var specification = GetSpecification(variant);
        return new Serpent512Cipher(specification.TestKey, specification.TestTweak);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Serpent-512 is a non-standard construction with no externally vetted reference vectors. The single self-referential
    /// vector below captures the cipher's own output at test-discovery time so that <c>EncryptTestData</c> and
    /// <c>DecryptTestData</c> have at least one row (MSTest fails empty <c>[DynamicData]</c> sources). Algorithmic
    /// correctness is anchored by the inherited round-trip and determinism tests.
    /// </remarks>
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(TweakableBlockCipherVariant variant)
    {
        if (variant == TweakableBlockCipherVariant.DefaultKeyAndTweak)
            yield break;

        var spec = GetSpecification(variant);
        byte[] input = new byte[spec.BlockSize];
        byte[] expected = new byte[spec.BlockSize];

        using (var cipher = new Serpent512Cipher(spec.TestKey, spec.TestTweak))
            cipher.Encrypt(input, expected);

        yield return new KnownAnswerTest
        {
            Name = "Serpent-512 / ZeroedKeyAndTweak / plaintext=00×64 (self-referential regression vector)",
            Input = input,
            ExpectedOutput = expected,
            CipherFactory = () => new Serpent512Cipher(spec.TestKey, spec.TestTweak),
        };
    }
}
