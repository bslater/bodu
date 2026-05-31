// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaBlockCipherTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="CamelliaBlockCipher" /> implementation against the three key-size variants (128, 192,
/// and 256 bits), including RFC 3713 known-answer test vectors and round-trip correctness checks.
/// </summary>
[TestClass]
internal sealed partial class CamelliaBlockCipherTests
    : BlockCipherTests<CamelliaBlockCipherTests, CamelliaBlockCipher, BlockCipherKeyVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(BlockCipherKeyVariant variant)
    {
        var keySize = variant switch
        {
            BlockCipherKeyVariant.Key128 => 16,
            BlockCipherKeyVariant.Key192 => 24,
            BlockCipherKeyVariant.Key256 => 32,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };

        return new BlockCipherSpecification
        {
            BlockSize = 16,
            KeySize = keySize,
            TestKey = TestHelpers.GenerateIncrementalByteSequence(0, keySize),
        };
    }

    /// <inheritdoc />
    protected override CamelliaBlockCipher CreateBlockCipher(BlockCipherKeyVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        return new CamelliaBlockCipher(spec.TestKey!);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(BlockCipherKeyVariant variant) =>
        KnownAnswersFor(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new CamelliaBlockCipher(answer.Key!);
}
