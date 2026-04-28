// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="CamelliaBlockCipher" /> implementation against the three key-size variants (128, 192,
/// and 256 bits), including RFC 3713 known-answer test vectors and round-trip correctness checks.
/// </summary>
[TestClass]
internal sealed partial class CamelliaBlockCipherTests
    : BlockCipherTests<CamelliaBlockCipherTests, CamelliaBlockCipher, CamelliaTestVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(CamelliaTestVariant variant)
    {
        int keySize = variant switch
        {
            CamelliaTestVariant.Key128 => 16,
            CamelliaTestVariant.Key192 => 24,
            CamelliaTestVariant.Key256 => 32,
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
    public override IEnumerable<CamelliaTestVariant> GetBlockCipherVariants() => new[]
    {
        CamelliaTestVariant.Key128,
        CamelliaTestVariant.Key192,
        CamelliaTestVariant.Key256,
    };

    /// <inheritdoc />
    protected override CamelliaBlockCipher CreateBlockCipher(CamelliaTestVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        return new CamelliaBlockCipher(spec.TestKey!);
    }

    /// <inheritdoc />
    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(CamelliaTestVariant variant) =>
        AdaptKnownAnswers(
            CamelliaKnownAnswers.For(variant),
            answer => new CamelliaBlockCipher(answer.Key!));
}
