using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Regression tests guarding against a set of defects previously identified in the
    /// block cipher mode transforms and the mode factory.
    /// </summary>
    [TestClass]
    public sealed class BlockCipherModeDefectTests
    {
        /// <summary>
        /// Minimal test-only <see cref="IBlockCipher" /> that copies input to output. The mode
        /// transforms only require a functioning encrypt/decrypt primitive for these tests; a
        /// real cipher is unnecessary.
        /// </summary>
        private sealed class FakeCipher : IBlockCipher
        {
            public int BlockSize { get; }

            public FakeCipher(int blockSize) => BlockSize = blockSize;

            public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output) => input.CopyTo(output);

            public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output) => input.CopyTo(output);

            public void Dispose() { }
        }

        [TestMethod]
        public void BlockCipherModeFactory_Create_WithUnsupportedMode_ShouldThrowWithCleanMessage()
        {
            using var cipher = new FakeCipher(16);
            byte[] iv = new byte[16];

            Exception? caught = null;
            try
            {
                BlockCipherModeFactory.Create((CipherBlockMode)999, cipher, iv);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.IsNotNull(caught, "Expected Create to throw for an unsupported mode.");
            Assert.IsFalse(
                caught!.Message.Contains("this."),
                $"Exception message should not contain 'this.' but was: {caught.Message}");
        }

        [TestMethod]
        public void BlockCipherModeFactory_Create_WithMissingIv_ShouldThrowWithCleanMessage()
        {
            using var cipher = new FakeCipher(16);

            Exception? caught = null;
            try
            {
                BlockCipherModeFactory.Create(CipherBlockMode.CBC, cipher, iv: null);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.IsNotNull(caught, "Expected Create to throw when IV is null for a mode that requires one.");
            Assert.IsFalse(
                caught!.Message.Contains("this."),
                $"Exception message should not contain 'this.' but was: {caught.Message}");
        }

        [TestMethod]
        public void OfbModeTransform_Ctor_WithNullIv_ShouldThrowArgumentNullExceptionWithIvParamName()
        {
            var cipher = new FakeCipher(16);

            var ex = Assert.ThrowsExactly<ArgumentNullException>(
                () => _ = new OfbModeTransform(cipher, null!));

            Assert.AreEqual("iv", ex.ParamName);
        }

        [TestMethod]
        public void CbcModeTransform_Ctor_WithBadIvLength_ShouldThrowArgumentException()
        {
            var cipher = new FakeCipher(16);
            byte[] iv = new byte[8];

            var ex = Assert.ThrowsExactly<ArgumentException>(
                () => _ = new CbcModeTransform(cipher, iv));

            Assert.AreEqual("iv", ex.ParamName);
        }

        [TestMethod]
        public void CfbModeTransform_Ctor_WithBadIvLength_ShouldThrowArgumentException()
        {
            var cipher = new FakeCipher(16);
            byte[] iv = new byte[8];

            var ex = Assert.ThrowsExactly<ArgumentException>(
                () => _ = new CfbModeTransform(cipher, iv));

            Assert.AreEqual("iv", ex.ParamName);
        }

        [TestMethod]
        public void OfbModeTransform_Ctor_WithBadIvLength_ShouldThrowArgumentException()
        {
            var cipher = new FakeCipher(16);
            byte[] iv = new byte[8];

            var ex = Assert.ThrowsExactly<ArgumentException>(
                () => _ = new OfbModeTransform(cipher, iv));

            Assert.AreEqual("iv", ex.ParamName);
        }

        [TestMethod]
        public void CtrModeTransform_WhenCounterWouldWrap_ShouldThrowCryptographicException()
        {
            // Use blockSize = 1 so the counter wraps after exactly 256 increments.
            // The 257th Transform call must refuse to produce a colliding keystream.
            using var cipher = new FakeCipher(1);
            byte[] initialCounter = new byte[1] { 0 };
            var ctr = new CtrModeTransform(cipher, initialCounter);

            byte[] inputBlock = new byte[1];
            byte[] outputBlock = new byte[1];

            for (int i = 0; i < 256; i++)
            {
                ctr.Transform(inputBlock, outputBlock, encrypt: true);
            }

            Assert.ThrowsExactly<CryptographicException>(
                () => ctr.Transform(inputBlock, outputBlock, encrypt: true));
        }
    }
}
