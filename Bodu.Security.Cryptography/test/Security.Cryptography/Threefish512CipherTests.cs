// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish512CipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising <see cref="Threefish512Cipher" /> at the block-cipher tier through the shared
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> harness, parameterised over
/// <see cref="TweakableBlockCipherVariant" />.
/// </summary>
[TestClass]
internal sealed class Threefish512CipherTests
    : BlockCipherTests<Threefish512CipherTests, Threefish512Cipher, TweakableBlockCipherVariant>
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
    protected override Threefish512Cipher CreateBlockCipher(TweakableBlockCipherVariant variant)
    {
        var spec = GetSpecification(variant);
        return new Threefish512Cipher(spec.TestKey, spec.TestTweak);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(TweakableBlockCipherVariant variant) =>
        Threefish512KnownAnswers.For(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new Threefish512Cipher(answer.Key!, answer.Tweak!);
}
