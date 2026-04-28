// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
internal sealed partial class BlowfishBlockCipherTests
    : BlockCipherTests<BlowfishBlockCipherTests, BlowfishBlockCipher, SingleTestVariant>
{
    /// <inerhitdocs/>
    protected override BlockCipherSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        BlockSize = 8,
        KeySize = 8,
        TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0, 8),
    };

    /// <inerhitdocs/>
    public override IEnumerable<SingleTestVariant> GetBlockCipherVariants() => new[]
    {
        SingleTestVariant.Default
    };

    /// <inerhitdocs/>
    protected override BlowfishBlockCipher CreateBlockCipher(SingleTestVariant variant)
    {
        var specification = GetSpecification(variant);
        return new BlowfishBlockCipher(specification.TestKey);
    }

    /// <inerhitdocs/>
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(SingleTestVariant variant) =>
        AdaptKnownAnswers(
            BlowfishKnownAnswers.For(variant),
            answer => new BlowfishBlockCipher(answer.Key!));
}
