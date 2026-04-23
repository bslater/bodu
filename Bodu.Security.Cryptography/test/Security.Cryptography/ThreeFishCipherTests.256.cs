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
    protected override BlockCipherSpecification GetSpecification(ThreeFishCipherTestVariant variant) =>
        variant switch
        {
            ThreeFishCipherTestVariant.ZeroedKeyAndTweak => new()
            {
                BlockSize = 32,
                KeySize = 32,
                TweakSize = 16,
                TestKey = new byte[32],
                TestTweak = new byte[16],
            },
            ThreeFishCipherTestVariant.DefaultKeyAndTweak => new()
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

    protected override Threefish256Cipher CreateBlockCipher(ThreeFishCipherTestVariant variant)
    {
        var spec = GetSpecification(variant);
        return new Threefish256Cipher(spec.TestKey, spec.TestTweak);
    }

    protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(ThreeFishCipherTestVariant variant) =>
        variant switch
        {
            ThreeFishCipherTestVariant.ZeroedKeyAndTweak =>
            [
                new KnownAnswerTest
                {
                    Name           = "Threefish-256 / key=00×32 tweak=00×16 / plaintext=00×32",
                    Input          = new byte[32],
                    ExpectedOutput = Convert.FromHexString("84DA2A1F8BEAEE947066AE3E3103F1AD536DB1F4A1192495116B9F3CE6133FD8"),
                    CipherFactory  = () => CreateBlockCipher(variant),
                }
            ],
            ThreeFishCipherTestVariant.DefaultKeyAndTweak =>
            [
                new KnownAnswerTest
                {
                    Name           = "Threefish-256 / key=00..1F tweak=10..1F / plaintext=FF..E0",
                    Input          = Convert.FromHexString("FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0"),
                    ExpectedOutput = Convert.FromHexString("E0D091FF0EEA8FDFC98192E62ED80AD59D865D08588DF476657056B5955E97DF"),
                    CipherFactory  = () => CreateBlockCipher(variant),
                }
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
}
