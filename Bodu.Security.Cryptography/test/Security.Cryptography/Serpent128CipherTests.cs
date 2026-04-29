// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128CipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising <see cref="Serpent128Cipher" /> against the canonical Serpent test vectors published by the
/// Bouncy Castle project (derived from the Anderson / Biham / Knudsen AES submission reference implementation).
/// </summary>
/// <remarks>
/// Serpent-128 is the standardised, non-tweakable Serpent variant — distinct from the experimental wide-block
/// Serpent-256 / 512 / 1024 constructions which are tweakable. It therefore plugs into
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> with <see cref="SingleTestVariant" /> rather than
/// <see cref="TweakableBlockCipherVariant" />.
/// </remarks>
[TestClass]
internal sealed class Serpent128CipherTests
    : BlockCipherTests<Serpent128CipherTests, Serpent128Cipher, SingleTestVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        BlockSize = 16,
        KeySize = 16,
        TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0x00, 16),
    };

    /// <inheritdoc />
    protected override Serpent128Cipher CreateBlockCipher(SingleTestVariant variant)
    {
        var specification = GetSpecification(variant);
        return new Serpent128Cipher(specification.TestKey);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(SingleTestVariant variant) =>
        Serpent128KnownAnswers.For(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new Serpent128Cipher(answer.Key!);
}
