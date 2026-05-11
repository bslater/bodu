// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent1024CipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising the wide-block tweakable <see cref="Serpent1024Cipher" /> engine.
/// </summary>
/// <remarks>
/// <para>
/// Serpent-1024 is a non-standard construction with no published reference vectors. End-to-end correctness is covered by the
/// inherited round-trip tests in <see cref="BlockCipherTests{TTest, TCipher, TVariant}" />.
/// </para>
/// <para>
/// TODO(gh-142): Decide vector strategy for wide-block Serpent. See <see cref="BlockCipherKnownAnswer" /> for the gap policy.
/// </para>
/// </remarks>
[TestClass]
internal sealed class Serpent1024CipherTests
    : BlockCipherTests<Serpent1024CipherTests, Serpent1024Cipher, TweakableBlockCipherVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(TweakableBlockCipherVariant variant) =>
        variant switch
        {
            TweakableBlockCipherVariant.ZeroedKeyAndTweak => new()
            {
                BlockSize = 128,
                KeySize = 128,
                TweakSize = 16,
                TestKey = new byte[128],
                TestTweak = new byte[16],
            },
            TweakableBlockCipherVariant.DefaultKeyAndTweak => new()
            {
                BlockSize = 128,
                KeySize = 128,
                TweakSize = 16,
                TestKey = TestHelpers.GenerateIncrementalByteSequence(0x10, 128),
                TestTweak = TestHelpers.GenerateIncrementalByteSequence(0, 16),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override Serpent1024Cipher CreateBlockCipher(TweakableBlockCipherVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        return new Serpent1024Cipher(specification.TestKey, specification.TestTweak);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Serpent-1024 is a non-standard construction with no externally vetted reference vectors. The single self-referential
    /// vector below captures the cipher's own output at test-discovery time so that <c>EncryptTestData</c> and
    /// <c>DecryptTestData</c> have at least one row (MSTest fails empty <c>[DynamicData]</c> sources). Algorithmic
    /// correctness is anchored by the inherited round-trip and determinism tests.
    /// </remarks>
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(TweakableBlockCipherVariant variant)
    {
        if (variant == TweakableBlockCipherVariant.DefaultKeyAndTweak)
            yield break;

        BlockCipherSpecification spec = GetSpecification(variant);
        byte[] input = new byte[spec.BlockSize];
        byte[] expected = new byte[spec.BlockSize];

        using (var cipher = new Serpent1024Cipher(spec.TestKey, spec.TestTweak))
            cipher.Encrypt(input, expected);

        yield return new KnownAnswerTest
        {
            Name = "Serpent-1024 / ZeroedKeyAndTweak / plaintext=00×128 (self-referential regression vector)",
            Input = input,
            ExpectedOutput = expected,
            CipherFactory = () => new Serpent1024Cipher(spec.TestKey, spec.TestTweak),
        };
    }
}
