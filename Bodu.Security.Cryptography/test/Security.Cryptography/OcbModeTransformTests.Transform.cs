namespace Bodu.Security.Cryptography
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Bodu.Testing.Security;

    public sealed partial class OcbModeTransformTests
    {
        // OcbModeTransform now implements IAeadBlockCipherModeTransform.
        // All tests use Encrypt / Decrypt — the old Transform(span, span, bool) no longer exists.

        [TestMethod]
        public void EncryptThenDecrypt_WithSameKeyAndNonce_ShouldRecoverPlaintext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = Enumerable.Repeat((byte)0x11, ExpectedBlockSize).ToArray();
            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();

            var enc = CreateTransform(cipher, (byte[])iv.Clone());
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);

            var dec = CreateTransform(cipher, (byte[])iv.Clone());
            var recovered = new byte[plaintext.Length];
            dec.Decrypt(ct, recovered);

            CollectionAssert.AreEqual(plaintext, recovered, "OCB round-trip must recover the original plaintext.");
        }

        [TestMethod]
        public void Encrypt_WithEmptyPlaintext_ShouldProduceTagOnly()
        {
            var enc = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00), new byte[ExpectedBlockSize]);
            var output = new byte[enc.TagSize];
            int n = enc.Encrypt(ReadOnlySpan<byte>.Empty, output);
            Assert.AreEqual(enc.TagSize, n, "Encrypting empty plaintext must write exactly TagSize bytes.");
        }

        [TestMethod]
        public void Decrypt_WhenCiphertextIsTampered_ShouldThrowCryptographicException()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
            var iv = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
            var plaintext = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)i).ToArray();

            var enc = CreateTransform(cipher, (byte[])iv.Clone());
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);
            ct[0] ^= 0xFF;

            var dec = CreateTransform(cipher, (byte[])iv.Clone());
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[plaintext.Length]);
            });
        }

        [TestMethod]
        public void Decrypt_WhenTagIsTampered_ShouldThrowCryptographicException()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = new byte[ExpectedBlockSize];
            var plaintext = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var enc = CreateTransform(cipher, (byte[])iv.Clone());
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);
            ct[ct.Length - 1] ^= 0x01;

            var dec = CreateTransform(cipher, (byte[])iv.Clone());
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[plaintext.Length]);
            });
        }

        [TestMethod]
        public void Decrypt_WhenAadIsTampered_ShouldThrowCryptographicException()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
            var iv = new byte[ExpectedBlockSize];
            var aad = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
            var plaintext = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)i).ToArray();

            var enc = CreateTransform(cipher, (byte[])iv.Clone());
            enc.ProcessAssociatedData(aad);
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);

            var badAad = (byte[])aad.Clone(); badAad[0] ^= 0xFF;
            var dec = CreateTransform(cipher, (byte[])iv.Clone());
            dec.ProcessAssociatedData(badAad);
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[plaintext.Length]);
            });
        }

        [TestMethod]
        public void Encrypt_WithDifferentNonces_ShouldProduceDifferentCiphertext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var plaintext = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)i).ToArray();
            var ivA = new byte[ExpectedBlockSize];
            var ivB = new byte[ExpectedBlockSize]; ivB[0] = 0x01;

            var encA = CreateTransform(cipher, ivA);
            var encB = CreateTransform(cipher, ivB);
            var ctA = new byte[plaintext.Length + encA.TagSize];
            var ctB = new byte[plaintext.Length + encB.TagSize];
            encA.Encrypt(plaintext, ctA);
            encB.Encrypt(plaintext, ctB);

            CollectionAssert.AreNotEqual(ctA, ctB, "Different nonces must produce different OCB ciphertext.");
        }

        [TestMethod]
        public void Encrypt_OutputLength_ShouldBePlaintextLengthPlusTagSize()
        {
            var enc = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00), new byte[ExpectedBlockSize]);
            var pt = new byte[ExpectedBlockSize * 3];
            var output = new byte[pt.Length + enc.TagSize];
            int n = enc.Encrypt(pt, output);
            Assert.AreEqual(pt.Length + enc.TagSize, n);
        }
    }
}