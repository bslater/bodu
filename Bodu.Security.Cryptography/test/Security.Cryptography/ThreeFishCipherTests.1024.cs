// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreeFishCipherTests.1024.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
internal partial class ThreeFish1024CipherTests
    : ThreeFishCipherTests<ThreeFish1024CipherTests, Threefish1024Cipher>
{
    protected override BlockCipherSpecification GetSpecification(ThreeFishCipherTestVariant variant) =>
        variant switch
        {
            ThreeFishCipherTestVariant.ZeroedKeyAndTweak => new()
            {
                BlockSize = 128,
                KeySize = 128,
                TweakSize = 16,
                TestKey = new byte[128],
                TestTweak = new byte[16],
            },
            ThreeFishCipherTestVariant.DefaultKeyAndTweak => new()
            {
                BlockSize = 128,
                KeySize = 128,
                TweakSize = 16,
                TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0x10, 128),
                TestTweak = CryptoTestUtilities.CreateIncrementalByteSequence(0, 16),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };

    protected override Threefish CreateInitialisedAlgorithm()
    {
        var algo = Threefish1024.Create();
        algo.GenerateKey();
        algo.GenerateIV();
        algo.GenerateTweak();
        return algo;
    }

    protected override Threefish1024Cipher CreateBlockCipher(ThreeFishCipherTestVariant variant)
    {
        var spec = GetSpecification(variant);
        return new Threefish1024Cipher(spec.TestKey, spec.TestTweak);
    }

    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(ThreeFishCipherTestVariant variant) =>
        AdaptKnownAnswers(
            Threefish1024KnownAnswers.For(variant),
            answer => new Threefish1024Cipher(answer.Key!, answer.Tweak!));
}
