using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    internal partial class ThreeFish512CipherTests
        : ThreeFishCipherTests<ThreeFish512CipherTests, Threefish512Cipher>
    {
        protected override BlockCipherSpecification GetSpecification(ThreeFishCipherTestVariant variant) =>
            variant switch
            {
                ThreeFishCipherTestVariant.ZeroedKeyAndTweak => new()
                {
                    BlockSize = 64,
                    KeySize = 64,
                    TweakSize = 16,
                    TestKey = new byte[64],
                    TestTweak = new byte[16],
                },
                ThreeFishCipherTestVariant.DefaultKeyAndTweak => new()
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

        protected override Threefish512Cipher CreateBlockCipher(ThreeFishCipherTestVariant variant)
        {
            var spec = this.GetSpecification(variant);
            return new Threefish512Cipher(spec.TestKey, spec.TestTweak);
        }

        protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(ThreeFishCipherTestVariant variant) =>
            variant switch
            {
                ThreeFishCipherTestVariant.ZeroedKeyAndTweak =>
                [
                    new KnownAnswerTest
                    {
                        Name           = "Threefish-512 / key=00×64 tweak=00×16 / plaintext=00×64",
                        Input          = new byte[64],
                        ExpectedOutput = Convert.FromHexString(
                            "b1a2bbc6ef6025bc40eb3822161f36e375d1bb0aee3186fbd19e47c5d479947b" +
                            "7bc2f8586e35f0cff7e7f03084b0b7b1f1ab3961a580a3e97eb41ea14a6d7bbe"),
                        CipherFactory  = () => CreateBlockCipher(variant),
                    }
                ],
                ThreeFishCipherTestVariant.DefaultKeyAndTweak =>
                [
                    new KnownAnswerTest
                    {
                        Name           = "Threefish-512 / key=00..3F tweak=10..1F / plaintext=FF..C0",
                        Input          = Convert.FromHexString(
                            "fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0efeeedecebeae9e8e7e6e5e4e3e2e1e0" +
                            "dfdedddcdbdad9d8d7d6d5d4d3d2d1d0cfcecdcccbcac9c8c7c6c5c4c3c2c1c0"),
                        ExpectedOutput = Convert.FromHexString(
                            "e304439626d45a2cb401cad8d636249a6338330eb06d45dd8b36b90e97254779" +
                            "272a0a8d99463504784420ea18c9a725af11dffea10162348927673d5c1caf3d"),
                        CipherFactory  = () => CreateBlockCipher(variant),
                    }
                ],
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
            };
    }
}