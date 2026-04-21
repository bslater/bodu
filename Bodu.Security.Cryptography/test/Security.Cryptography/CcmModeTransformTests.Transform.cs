// ---------------------------------------------------------------------------------------------------------------
// CcmModeTransformTests.Transform.cs
// ---------------------------------------------------------------------------------------------------------------
namespace Bodu.Security.Cryptography
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public sealed partial class CcmModeTransformTests
    {
        /// <summary>
        /// Verifies that CCM produces different ciphertext for the same plaintext under different nonces.
        /// </summary>
        [TestMethod]
        public void Encrypt_WithDifferentNonces_ShouldProduceDifferentCiphertext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var plaintext = new byte[ExpectedBlockSize];

            var iv1 = new byte[ExpectedBlockSize]; iv1[0] = 0x01;
            var iv2 = new byte[ExpectedBlockSize]; iv2[0] = 0x02;

            var t1 = new CcmModeTransform(cipher, iv1);
            var out1 = new byte[plaintext.Length + t1.TagSize];
            t1.Encrypt(plaintext, out1);

            var t2 = new CcmModeTransform(cipher, iv2);
            var out2 = new byte[plaintext.Length + t2.TagSize];
            t2.Encrypt(plaintext, out2);

            CollectionAssert.AreNotEqual(out1[..plaintext.Length], out2[..plaintext.Length],
                "CCM must produce different ciphertext for different nonces.");
        }

        /// <summary>
        /// Verifies that the CCM authentication tag changes when the plaintext changes.
        /// </summary>
        [TestMethod]
        public void Encrypt_WithDifferentPlaintext_ShouldProduceDifferentTag()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = new byte[ExpectedBlockSize];

            var pt1 = new byte[ExpectedBlockSize]; pt1[0] = 0xAA;
            var t1 = new CcmModeTransform(cipher, iv);
            var out1 = new byte[pt1.Length + t1.TagSize];
            t1.Encrypt(pt1, out1);

            var pt2 = new byte[ExpectedBlockSize]; pt2[0] = 0xBB;
            var t2 = new CcmModeTransform(cipher, (byte[])iv.Clone());
            var out2 = new byte[pt2.Length + t2.TagSize];
            t2.Encrypt(pt2, out2);

            CollectionAssert.AreNotEqual(out1[pt1.Length..], out2[pt2.Length..],
                "CCM must produce different tags for different plaintexts.");
        }

        /// <summary>
        /// Verifies CCM round-trip with three blocks of plaintext to exercise the multi-block CTR path.
        /// </summary>
        [TestMethod]
        public void EncryptThenDecrypt_WithMultipleBlocks_ShouldRecoverPlaintext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xBB);
            var iv = new byte[ExpectedBlockSize];
            var plaintext = new byte[ExpectedBlockSize * 3];
            for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i * 7);

            var encT = new CcmModeTransform(cipher, iv);
            var buf = new byte[plaintext.Length + encT.TagSize];
            encT.Encrypt(plaintext, buf);

            var decT = new CcmModeTransform(cipher, iv);
            var recovered = new byte[plaintext.Length];
            decT.Decrypt(buf, recovered);

            CollectionAssert.AreEqual(plaintext, recovered, "CCM must recover multi-block plaintext.");
        }
    }
}