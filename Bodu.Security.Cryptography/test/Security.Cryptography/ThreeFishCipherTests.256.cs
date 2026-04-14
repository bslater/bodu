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
    internal partial class ThreeFish256CipherTests
        : ThreeFishCipherTests<ThreeFish256CipherTests, Threefish256Cipher>
    {
        protected override int ExpectedBlockSize => 32;

        protected override Threefish CreateInitialisedAlgorithm()
        {
            var algo = Threefish256.Create();
            algo.GenerateKey();
            algo.GenerateIV();
            algo.GenerateTweak();
            return algo;
        }

        /// <summary>
        /// Verifies that encrypting a full block does not throw. Critical regression guard for
        /// Threefish-1024, where <c>rot[128]</c> out-of-bounds previously broke every call.
        /// </summary>
        [TestMethod]
        public void Encrypt_WhenFullBlockIsTransformed_ShouldNotThrow()
            => this.VerifyEncryptFullBlockDoesNotThrow();

        /// <summary>
        /// Verifies that encrypt/decrypt round-trips return the original plaintext.
        /// </summary>
        [TestMethod]
        public void EncryptDecrypt_RoundTrip_ShouldReturnOriginalPlaintext()
            => this.VerifyEncryptDecryptRoundTripReturnsOriginalPlaintext();

        protected override Threefish256Cipher CreateBlockCipher(ThreeFishCipherTestVariant variant) =>
            variant switch
            {
                ThreeFishCipherTestVariant.ZeroedKeyAndTweak => new Threefish256Cipher(new byte[ExpectedBlockSize], new byte[16]),
                ThreeFishCipherTestVariant.DefaultKeyAndTweak => new Threefish256Cipher(
                    key: Convert.FromHexString("101112131415161718191A1B1C1D1E1F202122232425262728292A2B2C2D2E2F"),
                    tweak: Convert.FromHexString("000102030405060708090A0B0C0D0E0F")
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
                        ExpectedOutput = Convert.FromHexString("84DA2A1F8BEAEE947066AE3E3103F1AD536DB1F4A1192495116B9F3CE6133FD8")
                    };
                    break;

                case ThreeFishCipherTestVariant.DefaultKeyAndTweak:
                    yield return new KnownAnswerTest
                    {
                        Name = "Set Key & Tweak",
                        Input = Convert.FromHexString("FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0"),
                        ExpectedOutput = Convert.FromHexString("E0D091FF0EEA8FDFC98192E62ED80AD59D865D08588DF476657056B5955E97DF")
                    };
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(variant));
            }
        }
    }
}