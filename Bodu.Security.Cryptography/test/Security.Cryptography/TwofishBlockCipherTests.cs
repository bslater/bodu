// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishBlockCipherTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

[TestClass]
internal sealed partial class TwofishBlockCipherTests
    : BlockCipherTests<TwofishBlockCipherTests, TwofishBlockCipher, BlockCipherKeyVariant>
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
    protected override TwofishBlockCipher CreateBlockCipher(BlockCipherKeyVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        return new TwofishBlockCipher(specification.TestKey);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(BlockCipherKeyVariant variant) =>
        KnownAnswersFor(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new TwofishBlockCipher(answer.Key!);
}
