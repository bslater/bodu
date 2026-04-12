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
        protected override int ExpectedBlockSize => 64;

        protected override Threefish512Cipher CreateBlockCipher(ThreeFishCipherTestVariant variant) =>
            variant switch
            {
                ThreeFishCipherTestVariant.ZeroedKeyAndTweak => new Threefish512Cipher(new byte[ExpectedBlockSize], new byte[16]),
                ThreeFishCipherTestVariant.DefaultKeyAndTweak => new Threefish512Cipher(
                    key: Convert.FromHexString("101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f" +
                                               "303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f"),
                    tweak: Convert.FromHexString("000102030405060708090a0b0c0d0e0f")
                ),

                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(ThreeFishCipherTestVariant variant)
        {
            switch (variant)
            {
                case ThreeFishCipherTestVariant.ZeroedKeyAndTweak:
                    yield return new KnownAnswerTest
                    {
                        Name = "Empty",
                        Input = new byte[ExpectedBlockSize],
                        ExpectedOutput = Convert.FromHexString("b1a2bbc6ef6025bc40eb3822161f36e375d1bb0aee3186fbd19e47c5d479947b" +
                                                               "7bc2f8586e35f0cff7e7f03084b0b7b1f1ab3961a580a3e97eb41ea14a6d7bbe")
                    };
                    break;

                case ThreeFishCipherTestVariant.DefaultKeyAndTweak:
                    yield return new KnownAnswerTest
                    {
                        Name = "Set Key & Tweak",
                        Input = Convert.FromHexString("fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0efeeedecebeae9e8e7e6e5e4e3e2e1e0" +
                                                      "dfdedddcdbdad9d8d7d6d5d4d3d2d1d0cfcecdcccbcac9c8c7c6c5c4c3c2c1c0"),
                        ExpectedOutput = Convert.FromHexString("e304439626d45a2cb401cad8d636249a6338330eb06d45dd8b36b90e97254779" +
                                                               "272a0a8d99463504784420ea18c9a725af11dffea10162348927673d5c1caf3d")
                    };
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(variant));
            }
        }
    }
}