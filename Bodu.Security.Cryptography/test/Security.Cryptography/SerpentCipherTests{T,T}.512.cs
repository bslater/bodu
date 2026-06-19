// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests{T,T}.512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising the wide-block tweakable <see cref="Serpent512Cipher" /> engine through the
/// shared <see cref="SerpentCipherTests{TTest, TCipher}" /> harness.
/// </summary>
/// <remarks>
/// <para>
/// Serpent-512 is a non-standard construction with no externally published reference vectors. The KAT rows used here are
/// cross-validated against an independent Python port of the wide-block round function (see
/// <c>tools/cipher-vectors/wide_serpent.py</c>), which is hand-translated from the C# source and exercises the same Serpent
/// S-boxes, bitsliced linear transform, prekey recurrence, cross-lane rotation, and five-word tweak schedule.
/// </para>
/// </remarks>
[TestClass]
internal sealed partial class Serpent512CipherTests
    : SerpentCipherTests<Serpent512CipherTests, Serpent512Cipher>
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
                TestKey = TestHelpers.GenerateIncrementalByteSequence(0x10, 64),
                TestTweak = TestHelpers.GenerateIncrementalByteSequence(0, 16),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override Serpent512Cipher CreateCipher(byte[] key, byte[] tweak) =>
        new(key, tweak);

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(TweakableBlockCipherVariant variant) =>
        KnownAnswersFor(variant);
}
