// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreeFishCipherTests.256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
internal partial class ThreeFish256CipherTests
    : ThreeFishCipherTests<ThreeFish256CipherTests, Threefish256Cipher>
{
    protected override BlockCipherSpecification GetSpecification(TweakableBlockCipherVariant variant) =>
        variant switch
        {
            TweakableBlockCipherVariant.ZeroedKeyAndTweak => new()
            {
                BlockSize = 32,
                KeySize = 32,
                TweakSize = 16,
                TestKey = new byte[32],
                TestTweak = new byte[16],
            },
            TweakableBlockCipherVariant.DefaultKeyAndTweak => new()
            {
                BlockSize = 32,
                KeySize = 32,
                TweakSize = 16,
                TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0x10, 32),
                TestTweak = CryptoTestUtilities.CreateIncrementalByteSequence(0, 16),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };

    protected override Threefish CreateInitialisedAlgorithm()
    {
        var algo = Threefish256.Create();
        algo.GenerateKey();
        algo.GenerateIV();
        algo.GenerateTweak();
        return algo;
    }

    protected override Threefish256Cipher CreateBlockCipher(TweakableBlockCipherVariant variant)
    {
        var spec = GetSpecification(variant);
        return new Threefish256Cipher(spec.TestKey, spec.TestTweak);
    }

    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(TweakableBlockCipherVariant variant) =>
        AdaptKnownAnswers(
            Threefish256KnownAnswers.For(variant),
            answer => new Threefish256Cipher(answer.Key!, answer.Tweak!));
}
