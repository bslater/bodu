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
            var spec = this.GetSpecification(variant);
            return new Threefish1024Cipher(spec.TestKey, spec.TestTweak);
        }

        protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(ThreeFishCipherTestVariant variant) =>
            variant switch
            {
                ThreeFishCipherTestVariant.ZeroedKeyAndTweak =>
                [
                    new KnownAnswerTest
                    {
                        Name           = "Threefish-1024 / key=00×128 tweak=00×16 / plaintext=00×128",
                        Input          = new byte[128],
                        ExpectedOutput = Convert.FromHexString(
                            "f05c3d0a3d05b304f785ddc7d1e036015c8aa76e2f217b06c6e1544c0bc1a90d" +
                            "f0accb9473c24e0fd54fea68057f43329cb454761d6df5cf7b2e9b3614fbd5a2" +
                            "0b2e4760b40603540d82eabc5482c171c832afbe68406bc39500367a592943fa" +
                            "9a5b4a43286ca3c4cf46104b443143d560a4b230488311df4feef7e1dfe8391e"),
                        CipherFactory  = () => CreateBlockCipher(variant),
                    }
                ],
                ThreeFishCipherTestVariant.DefaultKeyAndTweak =>
                [
                    new KnownAnswerTest
                    {
                        Name           = "Threefish-1024 / key=00..7F tweak=10..1F / plaintext=FF..80",
                        Input          = Convert.FromHexString(
                            "fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0efeeedecebeae9e8e7e6e5e4e3e2e1e0" +
                            "dfdedddcdbdad9d8d7d6d5d4d3d2d1d0cfcecdcccbcac9c8c7c6c5c4c3c2c1c0" +
                            "bfbebdbcbbbab9b8b7b6b5b4b3b2b1b0afaeadacabaaa9a8a7a6a5a4a3a2a1a0" +
                            "9f9e9d9c9b9a999897969594939291908f8e8d8c8b8a89888786858483828180"),
                        ExpectedOutput = Convert.FromHexString(
                            "a6654ddbd73cc3b05dd777105aa849bce49372eaaffc5568d254771bab85531c" +
                            "94f780e7ffaae430d5d8af8c70eebbe1760f3b42b737a89cb363490d670314bd" +
                            "8aa41ee63c2e1f45fbd477922f8360b388d6125ea6c7af0ad7056d01796e90c8" +
                            "3313f4150a5716b30ed5f569288ae974ce2b4347926fce57de44512177dd7cde"),
                        CipherFactory  = () => CreateBlockCipher(variant),
                    }
                ],
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
            };
    }
}