// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
internal sealed partial class SkipjackBlockCipherTests
    : BlockCipherTests<SkipjackBlockCipherTests, SkipjackBlockCipher, SingleTestVariant>
{
    /// <inheritdocs/>
    protected override BlockCipherSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        BlockSize = 8,
        KeySize = 10,
        TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0, 10),
    };

    /// <inheritdocs/>
    public override IEnumerable<SingleTestVariant> GetBlockCipherVariants() => new[]
    {
        SingleTestVariant.Default
    };

    /// <inheritdocs/>
    protected override SkipjackBlockCipher CreateBlockCipher(SingleTestVariant variant)
    {
        var specification = GetSpecification(variant);
        return new SkipjackBlockCipher(specification.TestKey);
    }

    /// <inheritdocs/>
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(SingleTestVariant variant) =>
        AdaptKnownAnswers(
            SkipjackKnownAnswers.For(variant),
            answer => new SkipjackBlockCipher(answer.Key!));
}
