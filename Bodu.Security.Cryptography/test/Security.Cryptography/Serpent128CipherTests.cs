// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128CipherTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class exercising <see cref="Serpent128Cipher" /> against the canonical Serpent test vectors from the
/// NESSIE submission by Ross Anderson, Eli Biham, and Lars Knudsen — the same corpus published with the original
/// Serpent AES candidate submission.
/// </summary>
/// <remarks>
/// Serpent-128 is the standardised, non-tweakable Serpent variant — distinct from the experimental wide-block
/// Serpent-256 / 512 / 1024 constructions which are tweakable. It therefore plugs into
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> with <see cref="SingleTestVariant" /> rather than
/// <see cref="TweakableBlockCipherVariant" />.
/// </remarks>
[TestClass]
internal sealed partial class Serpent128CipherTests
    : BlockCipherTests<Serpent128CipherTests, Serpent128Cipher, SingleTestVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        BlockSize = 16,
        KeySize = 16,
        TestKey = TestHelpers.GenerateIncrementalByteSequence(0x00, 16),
    };

    /// <inheritdoc />
    protected override Serpent128Cipher CreateBlockCipher(SingleTestVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        return new Serpent128Cipher(specification.TestKey);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(SingleTestVariant variant) =>
        KnownAnswersFor(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new Serpent128Cipher(answer.Key!);
}
