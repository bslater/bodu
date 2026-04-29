// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256CipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising <see cref="Threefish256Cipher" /> at the block-cipher tier through the shared
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> harness, parameterised over
/// <see cref="TweakableBlockCipherVariant" />.
/// </summary>
[TestClass]
internal sealed class Threefish256CipherTests
    : BlockCipherTests<Threefish256CipherTests, Threefish256Cipher, TweakableBlockCipherVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(TweakableBlockCipherVariant variant) =>
        variant switch
        {
            TweakableBlockCipherVariant.ZeroedKeyAndTweak => new()
            {
                BlockSize = 32,
                KeySize = 32,
                TweakSize = 16,
                TestKey = new byte[32],
                TestTweak = new byte[16],
            },
            TweakableBlockCipherVariant.DefaultKeyAndTweak => new()
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
    protected override Threefish256Cipher CreateBlockCipher(TweakableBlockCipherVariant variant)
    {
        var spec = GetSpecification(variant);
        return new Threefish256Cipher(spec.TestKey, spec.TestTweak);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(TweakableBlockCipherVariant variant) =>
        Threefish256KnownAnswers.For(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new Threefish256Cipher(answer.Key!, answer.Tweak!);
}
