// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishCipherTests{T,T}.512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising <see cref="Threefish512Cipher" /> at the block-cipher tier through the shared
/// <see cref="ThreefishCipherTests{TTest, TCipher}" /> harness, parameterised over
/// <see cref="TweakableBlockCipherVariant" />.
/// </summary>
[TestClass]
internal sealed partial class Threefish512CipherTests
    : ThreefishCipherTests<Threefish512CipherTests, Threefish512Cipher>
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
                KnownAnswers = KnownAnswersFor(variant),
                TestTweak = new byte[16],
            },
            TweakableBlockCipherVariant.DefaultKeyAndTweak => new()
            {
                BlockSize = 64,
                KeySize = 64,
                TweakSize = 16,
                TestKey = TestHelpers.GenerateIncrementalByteSequence(0x10, 64),
                KnownAnswers = KnownAnswersFor(variant),
                TestTweak = TestHelpers.GenerateIncrementalByteSequence(0, 16),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override Threefish512Cipher CreateCipher(byte[] key, byte[] tweak) =>
        new(key, tweak);

}
