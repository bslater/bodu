// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishSkipjackDefectTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Regression tests covering recently-fixed defects in the Blowfish and Skipjack cipher transforms.
    /// </summary>
    [TestClass]
    public class BlowfishSkipjackDefectTests
    {
        // Defect A: SkipjackBlockCipher constructor exception message contained a stray "this." prefix.
        [TestMethod]
        public void SkipjackBlockCipher_Ctor_WithBadKeyLength_ShouldThrowWithCleanMessage()
        {
            // Defect A regression: ensure the exception message for a wrong-length key
            // does not contain the stray "this." token that appeared in the original code.
            byte[] badKey = new byte[8]; // Skipjack requires exactly 10 bytes.

            ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
            {
                using var _ = new SkipjackBlockCipher(badKey);
            });

            Assert.IsFalse(
                ex.Message.Contains("this."),
                "Skipjack key-length exception message must not contain the stray 'this.' token.");
            Assert.IsTrue(
                ex.Message.Contains("80-bit key"),
                "Skipjack key-length exception message should still describe the required key size.");
        }

        // Defect B: SkipjackTransform.Dispose did not zero deferredInput nor dispose the inner cipher.
        [TestMethod]
        public void SkipjackTransform_Dispose_ShouldMakeTransformUnusable()
        {
            // Defect B regression: after Dispose the underlying cipher must be disposed,
            // so any further use of the transform should fail.
            using var alg = new Skipjack();
            alg.GenerateKey();
            alg.GenerateIV();

            ICryptoTransform encryptor = alg.CreateEncryptor(alg.Key, alg.IV);

            byte[] input = new byte[encryptor.InputBlockSize];
            byte[] output = new byte[encryptor.OutputBlockSize];
            encryptor.TransformBlock(input, 0, input.Length, output, 0);

            encryptor.Dispose();

            bool threw = false;
            try
            {
                encryptor.TransformBlock(input, 0, input.Length, output, 0);
            }
            catch (Exception)
            {
                threw = true;
            }

            Assert.IsTrue(threw, "SkipjackTransform should be unusable after Dispose.");
        }

        // Defect C: BlowfishTransform.Dispose did not zero the deferredInput buffer.
        [TestMethod]
        public void BlowfishTransform_Dispose_ShouldDisposeCipher_AndClearDeferredInput()
        {
            // Defect C regression: after Dispose, deferredInput must be cleared and the
            // underlying cipher disposed; reuse of the transform must fail.
            using var alg = Blowfish.Create();
            alg.GenerateKey();
            alg.GenerateIV();

            ICryptoTransform decryptor = alg.CreateDecryptor(alg.Key, alg.IV);

            // Feed a single block to populate internal state (deferredInput when decrypting with PKCS7).
            byte[] input = new byte[decryptor.InputBlockSize];
            byte[] output = new byte[decryptor.OutputBlockSize];
            decryptor.TransformBlock(input, 0, input.Length, output, 0);

            decryptor.Dispose();

            bool threw = false;
            try
            {
                decryptor.TransformBlock(input, 0, input.Length, output, 0);
            }
            catch (Exception)
            {
                threw = true;
            }

            Assert.IsTrue(threw, "BlowfishTransform should be unusable after Dispose.");
        }

        // Defect B: calling Dispose twice must be safe.
        [TestMethod]
        public void SkipjackTransform_Dispose_IsIdempotent()
        {
            // Defect B regression: Dispose should be idempotent and never throw.
            using var alg = new Skipjack();
            alg.GenerateKey();
            alg.GenerateIV();

            ICryptoTransform encryptor = alg.CreateEncryptor(alg.Key, alg.IV);

            encryptor.Dispose();

            bool threw = false;
            try
            {
                encryptor.Dispose();
            }
            catch (Exception)
            {
                threw = true;
            }

            Assert.IsFalse(threw, "Calling Dispose a second time on SkipjackTransform should not throw.");
        }

        // Defect C: calling Dispose twice must be safe.
        [TestMethod]
        public void BlowfishTransform_Dispose_IsIdempotent()
        {
            // Defect C regression: Dispose should be idempotent and never throw.
            using var alg = Blowfish.Create();
            alg.GenerateKey();
            alg.GenerateIV();

            ICryptoTransform encryptor = alg.CreateEncryptor(alg.Key, alg.IV);

            encryptor.Dispose();

            bool threw = false;
            try
            {
                encryptor.Dispose();
            }
            catch (Exception)
            {
                threw = true;
            }

            Assert.IsFalse(threw, "Calling Dispose a second time on BlowfishTransform should not throw.");
        }
    }
}
