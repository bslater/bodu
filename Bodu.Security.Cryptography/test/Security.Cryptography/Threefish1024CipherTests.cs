// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish1024CipherTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising <see cref="Threefish1024Cipher" /> at the block-cipher tier through the shared
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> harness, parameterised over
/// <see cref="TweakableBlockCipherVariant" />.
/// </summary>
[TestClass]
internal sealed class Threefish1024CipherTests
    : BlockCipherTests<Threefish1024CipherTests, Threefish1024Cipher, TweakableBlockCipherVariant>
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
    protected override Threefish1024Cipher CreateBlockCipher(TweakableBlockCipherVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        return new Threefish1024Cipher(spec.TestKey, spec.TestTweak);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(TweakableBlockCipherVariant variant) =>
        Threefish1024KnownAnswers.For(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new Threefish1024Cipher(answer.Key!, answer.Tweak!);
}
