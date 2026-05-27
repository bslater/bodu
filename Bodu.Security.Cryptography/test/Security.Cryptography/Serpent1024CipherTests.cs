// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent1024CipherTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising the wide-block tweakable <see cref="Serpent1024Cipher" /> engine.
/// </summary>
/// <remarks>
/// <para>
/// Serpent-1024 is a non-standard construction with no externally published reference vectors. The KAT rows used here are
/// cross-validated against an independent Python port of the wide-block round function (see
/// <c>tools/cipher-vectors/wide_serpent.py</c>), which is hand-translated from the C# source and exercises the same Serpent
/// S-boxes, bitsliced linear transform, prekey recurrence, cross-lane rotation, and five-word tweak schedule.
/// </para>
/// </remarks>
[TestClass]
internal sealed partial class Serpent1024CipherTests
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
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(TweakableBlockCipherVariant variant) =>
        KnownAnswersFor(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new Serpent1024Cipher(answer.Key!, answer.Tweak!);
}
