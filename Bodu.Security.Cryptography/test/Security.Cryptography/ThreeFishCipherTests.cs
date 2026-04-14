using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public enum ThreeFishCipherTestVariant
    {
        ZeroedKeyAndTweak,
        DefaultKeyAndTweak
    }

    internal abstract partial class ThreeFishCipherTests<TTest, TCipher>
        : BlockCipherTests<TTest, TCipher, ThreeFishCipherTestVariant>
        where TTest : ThreeFishCipherTests<TTest, TCipher>, new()
        where TCipher : ThreefishBlockCipher
    {
        public override IEnumerable<ThreeFishCipherTestVariant> GetBlockCipherVariants() =>
            Enum.GetValues<ThreeFishCipherTestVariant>().ToArray();

        /// <summary>
        /// Creates an initialised instance of the Threefish variant under test with a freshly
        /// generated key, IV, and tweak.
        /// </summary>
        protected abstract Threefish CreateInitialisedAlgorithm();

        /// <summary>
        /// Core reusable test body: encrypts a full block via the variant's encryptor and asserts
        /// the call completes without exception.
        /// </summary>
        protected void VerifyEncryptFullBlockDoesNotThrow()
        {
            using var algo = this.CreateInitialisedAlgorithm();
            using var encryptor = algo.CreateEncryptor(algo.Key, algo.IV, ((TweakableSymmetricAlgorithm)algo).Tweak);

            byte[] input = new byte[this.ExpectedBlockSize];
            for (int i = 0; i < input.Length; i++)
                input[i] = (byte)i;

            byte[] output = encryptor.TransformFinalBlock(input, 0, input.Length);

            Assert.IsNotNull(output);
            Assert.IsTrue(output.Length > 0, "Expected non-empty ciphertext output.");
        }

        /// <summary>
        /// Core reusable test body: encrypts a full block and decrypts it with a fresh decryptor,
        /// asserting the round-trip returns the original plaintext.
        /// </summary>
        protected void VerifyEncryptDecryptRoundTripReturnsOriginalPlaintext()
        {
            using var algo = this.CreateInitialisedAlgorithm();

            byte[] key = algo.Key;
            byte[] iv = algo.IV;
            byte[] tweak = ((TweakableSymmetricAlgorithm)algo).Tweak;

            byte[] plaintext = new byte[this.ExpectedBlockSize];
            for (int i = 0; i < plaintext.Length; i++)
                plaintext[i] = (byte)(i ^ 0x5A);

            byte[] ciphertext;
            using (var enc = algo.CreateEncryptor(key, iv, tweak))
                ciphertext = enc.TransformFinalBlock(plaintext, 0, plaintext.Length);

            byte[] roundTrip;
            using (var dec = algo.CreateDecryptor(key, iv, tweak))
                roundTrip = dec.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

            CollectionAssert.AreEqual(plaintext, roundTrip);
        }

    }
}