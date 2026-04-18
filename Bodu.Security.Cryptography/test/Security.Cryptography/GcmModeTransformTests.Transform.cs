// ---------------------------------------------------------------------------------------------------------------
// GcmModeTransformTests.Transform.cs
// ---------------------------------------------------------------------------------------------------------------
namespace Bodu.Security.Cryptography
{
    using Bodu.Testing.Security;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public sealed partial class GcmModeTransformTests
    {
        /// <summary>
        /// Verifies that GCM produces different ciphertext for the same plaintext under different nonces,
        /// confirming that the nonce is incorporated into the CTR keystream.
        /// </summary>
        [TestMethod]
        public void Encrypt_WithDifferentNonces_ShouldProduceDifferentCiphertext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var plaintext = new byte[ExpectedBlockSize];

            var iv1 = new byte[ExpectedBlockSize]; iv1[0] = 0x01;
            var iv2 = new byte[ExpectedBlockSize]; iv2[0] = 0x02;

            var t1 = new GcmModeTransform(cipher, iv1);
            var out1 = new byte[plaintext.Length + t1.TagSize];
            t1.Encrypt(plaintext, out1);

            var t2 = new GcmModeTransform(cipher, iv2);
            var out2 = new byte[plaintext.Length + t2.TagSize];
            t2.Encrypt(plaintext, out2);

            CollectionAssert.AreNotEqual(out1[..plaintext.Length], out2[..plaintext.Length],
                "GCM must produce different ciphertext for different nonces.");
        }

        /// <summary>
        /// Verifies that GCM produces a non-zero authentication tag for a non-trivial plaintext.
        /// </summary>
        [TestMethod]
        public void Encrypt_ShouldProduceNonZeroTag()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = new byte[ExpectedBlockSize];
            var plaintext = new byte[ExpectedBlockSize];
            plaintext[0] = 0x42;

            var transform = new GcmModeTransform(cipher, iv);
            var output = new byte[plaintext.Length + transform.TagSize];
            transform.Encrypt(plaintext, output);

            bool tagIsAllZero = true;
            for (int i = plaintext.Length; i < output.Length; i++)
                if (output[i] != 0) { tagIsAllZero = false; break; }

            Assert.IsFalse(tagIsAllZero, "GCM authentication tag must not be all-zero for non-trivial input.");
        }

        /// <summary>
        /// Verifies that different AAD values produce different authentication tags, confirming that
        /// the AAD is incorporated into the GHASH computation.
        /// </summary>
        [TestMethod]
        public void Encrypt_WithDifferentAad_ShouldProduceDifferentTag()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = new byte[ExpectedBlockSize];
            var plaintext = new byte[ExpectedBlockSize];

            var t1 = new GcmModeTransform(cipher, iv);
            t1.ProcessAssociatedData(new byte[] { 0x01 });
            var out1 = new byte[plaintext.Length + t1.TagSize];
            t1.Encrypt(plaintext, out1);

            var t2 = new GcmModeTransform(cipher, (byte[])iv.Clone());
            t2.ProcessAssociatedData(new byte[] { 0xFF });
            var out2 = new byte[plaintext.Length + t2.TagSize];
            t2.Encrypt(plaintext, out2);

            CollectionAssert.AreNotEqual(out1[plaintext.Length..], out2[plaintext.Length..],
                "GCM must produce different tags for different AAD values.");
        }
    }
}