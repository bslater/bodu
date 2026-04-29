// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
            TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0, keySize),
        };
    }

    /// <inheritdoc />
    public override IEnumerable<BlockCipherKeyVariant> GetBlockCipherVariants() => new[]
    {
        BlockCipherKeyVariant.Key128,
        BlockCipherKeyVariant.Key192,
        BlockCipherKeyVariant.Key256,
    };

    /// <inheritdoc />
    protected override TwofishBlockCipher CreateBlockCipher(BlockCipherKeyVariant variant)
    {
        var specification = GetSpecification(variant);
        return new TwofishBlockCipher(specification.TestKey);
    }

    /// <inheritdoc />
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(BlockCipherKeyVariant variant) =>
        AdaptKnownAnswers(
            TwofishKnownAnswers.For(variant),
            answer => new TwofishBlockCipher(answer.Key!));
}
