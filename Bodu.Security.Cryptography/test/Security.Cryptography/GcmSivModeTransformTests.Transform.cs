// ---------------------------------------------------------------------------------------------------------------
// GcmSivModeTransformTests.Transform.cs
// ---------------------------------------------------------------------------------------------------------------
namespace Bodu.Security.Cryptography
{
    using Bodu.Testing.Security;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public sealed partial class GcmSivModeTransformTests
    {
        /// <summary>
        /// Verifies that GCM-SIV produces the same ciphertext when encrypting the same plaintext
        /// with the same nonce twice — the mode is deterministic (misuse-resistant).
        /// </summary>
        [TestMethod]
        public void Encrypt_SamePlaintextSameNonce_ShouldProduceSameCiphertext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = new byte[ExpectedBlockSize];
            var plaintext = new byte[ExpectedBlockSize];
            plaintext[0] = 0x42;

            var t1 = new GcmSivModeTransform(cipher, iv);
            var out1 = new byte[plaintext.Length + t1.TagSize];
            t1.Encrypt(plaintext, out1);

            var t2 = new GcmSivModeTransform(cipher, (byte[])iv.Clone());
            var out2 = new byte[plaintext.Length + t2.TagSize];
            t2.Encrypt(plaintext, out2);

            CollectionAssert.AreEqual(out1, out2,
                "GCM-SIV must be deterministic: same plaintext + nonce always produces same ciphertext.");
        }

        /// <summary>
        /// Verifies that GCM-SIV produces different ciphertext for different plaintext under the same nonce.
        /// </summary>
        [TestMethod]
        public void Encrypt_DifferentPlaintext_ShouldProduceDifferentCiphertext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = new byte[ExpectedBlockSize];

            var pt1 = new byte[ExpectedBlockSize]; pt1[0] = 0xAA;
            var t1 = new GcmSivModeTransform(cipher, iv);
            var out1 = new byte[pt1.Length + t1.TagSize];
            t1.Encrypt(pt1, out1);

            var pt2 = new byte[ExpectedBlockSize]; pt2[0] = 0xBB;
            var t2 = new GcmSivModeTransform(cipher, (byte[])iv.Clone());
            var out2 = new byte[pt2.Length + t2.TagSize];
            t2.Encrypt(pt2, out2);

            CollectionAssert.AreNotEqual(out1, out2,
                "GCM-SIV must produce different ciphertext for different plaintexts.");
        }

        /// <summary>
        /// Verifies that GCM-SIV produces different tags for different AAD values with the same plaintext.
        /// </summary>
        [TestMethod]
        public void Encrypt_WithDifferentAad_ShouldProduceDifferentTag()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = new byte[ExpectedBlockSize];
            var plaintext = new byte[ExpectedBlockSize];

            var t1 = new GcmSivModeTransform(cipher, iv);
            t1.ProcessAssociatedData(new byte[] { 0x01 });
            var out1 = new byte[plaintext.Length + t1.TagSize];
            t1.Encrypt(plaintext, out1);

            var t2 = new GcmSivModeTransform(cipher, (byte[])iv.Clone());
            t2.ProcessAssociatedData(new byte[] { 0xFF });
            var out2 = new byte[plaintext.Length + t2.TagSize];
            t2.Encrypt(plaintext, out2);

            CollectionAssert.AreNotEqual(out1[plaintext.Length..], out2[plaintext.Length..],
                "GCM-SIV must produce different tags for different AAD values.");
        }

        /// <summary>
        /// Verifies that GCM-SIV round-trips with multiple blocks of plaintext.
        /// </summary>
        [TestMethod]
        public void EncryptThenDecrypt_WithMultipleBlocks_ShouldRecoverPlaintext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xBB);
            var iv = new byte[ExpectedBlockSize];
            var plaintext = new byte[ExpectedBlockSize * 4];
            for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i * 5 + 1);

            var encT = new GcmSivModeTransform(cipher, iv);
            var buf = new byte[plaintext.Length + encT.TagSize];
            encT.Encrypt(plaintext, buf);

            var decT = new GcmSivModeTransform(cipher, iv);
            var recovered = new byte[plaintext.Length];
            decT.Decrypt(buf, recovered);

            CollectionAssert.AreEqual(plaintext, recovered,
                "GCM-SIV must recover multi-block plaintext on decryption.");
        }
    }
}