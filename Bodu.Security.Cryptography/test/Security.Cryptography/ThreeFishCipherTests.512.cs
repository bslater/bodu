// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreeFishCipherTests.512.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
internal partial class ThreeFish512CipherTests
    : ThreeFishCipherTests<ThreeFish512CipherTests, Threefish512Cipher>
{
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
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };

    protected override Threefish CreateInitialisedAlgorithm()
    {
        var algo = Threefish512.Create();
        algo.GenerateKey();
        algo.GenerateIV();
        algo.GenerateTweak();
        return algo;
    }

    protected override Threefish512Cipher CreateBlockCipher(TweakableBlockCipherVariant variant)
    {
        var spec = GetSpecification(variant);
        return new Threefish512Cipher(spec.TestKey, spec.TestTweak);
    }

    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(TweakableBlockCipherVariant variant) =>
        AdaptKnownAnswers(
            Threefish512KnownAnswers.For(variant),
            answer => new Threefish512Cipher(answer.Key!, answer.Tweak!));
}
