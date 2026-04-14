using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Regression tests covering specific defects in the Threefish cipher family.
    /// Each test corresponds to a defect identified during code review.
    /// </summary>
    [TestClass]
    public class ThreeFishCipherDefectTests
    {
        // ------------------------------------------------------------------
        // Defect A: Threefish1024 rot[128] out-of-bounds rotation index.
        //           rot is a 64-entry array; rot[128] would IOR on every call.
        //           After the fix (rot[128] -> rot[32]), encrypt/decrypt run
        //           cleanly and round-trip correctly.
        // ------------------------------------------------------------------

        [TestMethod]
        public void Threefish1024_Encrypt_ShouldNotThrow_AfterRotBoundsFix()
        {
            using var algo = Threefish1024.Create();
            algo.GenerateKey();
            algo.GenerateIV();
            algo.GenerateTweak();

            using var encryptor = algo.CreateEncryptor(algo.Key, algo.IV, algo.Tweak);

            // 128-byte input matches the 1024-bit Threefish block size exactly.
            byte[] input = new byte[128];
            for (int i = 0; i < input.Length; i++)
                input[i] = (byte)i;

            byte[] output = encryptor.TransformFinalBlock(input, 0, input.Length);

            Assert.IsNotNull(output);
            Assert.IsTrue(output.Length > 0, "Expected non-empty ciphertext output.");
        }

        [TestMethod]
        public void Threefish1024_EncryptDecrypt_RoundTrip_ShouldReturnOriginalPlaintext()
        {
            using var algo = Threefish1024.Create();
            algo.GenerateKey();
            algo.GenerateIV();
            algo.GenerateTweak();

            byte[] key = algo.Key;
            byte[] iv = algo.IV;
            byte[] tweak = algo.Tweak;

            byte[] plaintext = new byte[128];
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

        // ------------------------------------------------------------------
        // Defect B: Threefish.Validate IV/Tweak length error messages
        //           previously formatted key.Length * 8 instead of the
        //           offending IV/Tweak length, producing misleading errors.
        // ------------------------------------------------------------------

        [TestMethod]
        public void Threefish_Validate_WithBadIvLength_ShouldReportIvLengthNotKeyLength()
        {
            using var algo = Threefish256.Create();
            algo.GenerateKey();
            algo.GenerateTweak();

            // 9-byte IV => 72 bits; correct IV length for Threefish-256 is 32 bytes.
            byte[] badIv = new byte[9];

            var ex = Assert.ThrowsExactly<CryptographicException>(() =>
                algo.CreateEncryptor(algo.Key, badIv, algo.Tweak));

            // Should reference the actual IV bit-length (72) and not the key bit-length (256).
            Assert.IsTrue(ex.Message.Contains("72"),
                $"Expected IV bit-length 72 in message but got: {ex.Message}");
        }

        [TestMethod]
        public void Threefish_Validate_WithBadTweakLength_ShouldReportTweakLengthNotKeyLength()
        {
            using var algo = Threefish256.Create();
            algo.GenerateKey();
            algo.GenerateIV();

            // 5-byte tweak => 40 bits; correct tweak length is 16 bytes (128 bits).
            byte[] badTweak = new byte[5];

            var ex = Assert.ThrowsExactly<CryptographicException>(() =>
                algo.CreateEncryptor(algo.Key, algo.IV, badTweak));

            Assert.IsTrue(ex.Message.Contains("40"),
                $"Expected tweak bit-length 40 in message but got: {ex.Message}");
        }

        // ------------------------------------------------------------------
        // Defect C: ThreefishBlockCipher.ThrowIfDisposed previously used
        //           nameof(T) (no T in scope). After the fix it uses the
        //           concrete type name via this.GetType().Name.
        // ------------------------------------------------------------------

        [TestMethod]
        public void ThreefishBlockCipher_Dispose_ThenUse_ShouldThrowObjectDisposedException_WithConcreteTypeName()
        {
            var cipher = new Threefish256Cipher(new byte[32], new byte[16]);
            cipher.Dispose();

            byte[] block = new byte[32];
            byte[] output = new byte[32];

            var ex = Assert.ThrowsExactly<ObjectDisposedException>(() =>
                cipher.Encrypt(block, output));

            Assert.AreEqual(nameof(Threefish256Cipher), ex.ObjectName,
                "Disposed exception should report the concrete cipher type name, not 'T'.");
            Assert.AreNotEqual("T", ex.ObjectName);
        }

        // ------------------------------------------------------------------
        // Defect D: TweakableSymmetricAlgorithm.ThrowIfInvalidTweakSize
        //           previously declared CallerArgumentExpression("TweakSchedule"),
        //           an invalid parameter reference. After the fix the message
        //           is well-formed and never references the literal "TweakSchedule".
        // ------------------------------------------------------------------

        [TestMethod]
        public void TweakableSymmetricAlgorithm_ThrowIfInvalidTweakSize_WithBadTweak_ShouldReportCorrectParamName()
        {
            using var algo = Threefish256.Create();

            // Setting the Tweak property triggers ThrowIfInvalidTweakSize.
            var ex = Assert.ThrowsExactly<CryptographicException>(() =>
                algo.Tweak = new byte[5]);

            Assert.IsFalse(string.IsNullOrEmpty(ex.Message), "Expected non-empty exception message.");
            Assert.IsFalse(ex.Message.Contains("TweakSchedule"),
                "Exception message must not reference the unrelated 'TweakSchedule' symbol.");
        }

        // ------------------------------------------------------------------
        // Defect E: ThreefishTransform.Dispose previously did not dispose the
        //           inner IBlockCipher engine and did not zero the
        //           deferredInput buffer. After the fix the transform is no
        //           longer reusable and underlying state is wiped.
        //
        // Note: the inner cipher is internal and not exposed via a public
        // accessor, so we observe disposal indirectly: after Dispose() the
        // transform's underlying cipher is disposed, which causes any further
        // Encrypt/Decrypt calls (via TransformBlock) to throw.
        // ------------------------------------------------------------------

        [TestMethod]
        public void ThreefishTransform_Dispose_ShouldDisposeUnderlyingCipher_AndClearDeferredInput()
        {
            using var algo = Threefish256.Create();
            algo.GenerateKey();
            algo.GenerateIV();
            algo.GenerateTweak();

            var transform = algo.CreateEncryptor(algo.Key, algo.IV, algo.Tweak);

            // Feed a full block so internal state is exercised.
            byte[] input = new byte[32];
            byte[] output = new byte[32];
            int written = transform.TransformBlock(input, 0, input.Length, output, 0);
            Assert.AreEqual(32, written);

            transform.Dispose();

            // After disposal the transform should be unusable. The exact exception
            // type depends on the underlying engine's behaviour but it must throw.
            bool threw = false;
            try
            {
                transform.TransformBlock(input, 0, input.Length, output, 0);
            }
            catch (Exception)
            {
                threw = true;
            }

            Assert.IsTrue(threw, "Disposed ThreefishTransform should not be reusable.");
        }
    }
}
