// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
internal sealed partial class TwofishBlockCipherTests
    : BlockCipherTests<TwofishBlockCipherTests, TwofishBlockCipher, TwofishTestVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(TwofishTestVariant variant)
    {
        var keySize = variant switch
        {
            TwofishTestVariant.Key128 => 16,
            TwofishTestVariant.Key192 => 24,
            TwofishTestVariant.Key256 => 32,
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
    public override IEnumerable<TwofishTestVariant> GetBlockCipherVariants() => new[]
    {
        TwofishTestVariant.Key128,
        TwofishTestVariant.Key192,
        TwofishTestVariant.Key256,
    };

    /// <inheritdoc />
    protected override TwofishBlockCipher CreateBlockCipher(TwofishTestVariant variant)
    {
        var specification = GetSpecification(variant);
        return new TwofishBlockCipher(specification.TestKey);
    }

    /// <inheritdoc />
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(TwofishTestVariant variant) =>
        AdaptKnownAnswers(
            TwofishKnownAnswers.For(variant),
            answer => new TwofishBlockCipher(answer.Key!));
}
