using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public class Pkcs7PaddingDefectTests
    {
        private const int BlockSize = 16;

        [TestMethod]
        public void Pkcs7Padding_Unpad_WithCorruptedPadding_ShouldThrowCryptographicExceptionWithCleanMessage()
        {
            var padding = new Pkcs7Padding();

            // 4-byte payload padded with 12 bytes of 0x0C, then corrupt one of the pad bytes.
            byte[] input = new byte[BlockSize];
            input[0] = 0x41;
            input[1] = 0x42;
            input[2] = 0x43;
            input[3] = 0x44;
            for (int i = 4; i < BlockSize; i++)
                input[i] = 0x0C;
            input[8] = 0x00; // tamper with a padding byte mid-run

            var ex = Assert.ThrowsExactly<CryptographicException>(() => padding.Unpad(input, BlockSize));
            Assert.IsFalse(ex.Message.Contains("this."), "Exception message must not contain 'this.' artifact.");
            Assert.IsFalse(ex.Message.Contains(" t "), "Exception message must not contain stray ' t ' sequence.");
        }

        [TestMethod]
        public void Pkcs7Padding_Unpad_WithValidPadding_ShouldReturnUnpaddedBytes()
        {
            var padding = new Pkcs7Padding();

            byte[] input = new byte[BlockSize];
            input[0] = 0x41;
            input[1] = 0x42;
            input[2] = 0x43;
            input[3] = 0x44;
            for (int i = 4; i < BlockSize; i++)
                input[i] = 0x0C;

            byte[] result = padding.Unpad(input, BlockSize);
            byte[] expected = new byte[] { 0x41, 0x42, 0x43, 0x44 };
            CollectionAssert.AreEqual(expected, result);

            // Round-trip via Pad then Unpad.
            byte[] original = new byte[] { 0x41, 0x42, 0x43, 0x44 };
            byte[] padded = padding.Pad(original, BlockSize);
            byte[] roundTripped = padding.Unpad(padded, BlockSize);
            CollectionAssert.AreEqual(original, roundTripped);
        }

        [TestMethod]
        public void Pkcs7Padding_Unpad_WithZeroPadLengthByte_ShouldThrowCryptographicException()
        {
            var padding = new Pkcs7Padding();

            byte[] input = new byte[BlockSize];
            for (int i = 0; i < BlockSize - 1; i++)
                input[i] = 0xAA;
            input[BlockSize - 1] = 0x00;

            var ex = Assert.ThrowsExactly<CryptographicException>(() => padding.Unpad(input, BlockSize));
            Assert.IsFalse(ex.Message.Contains("this."), "Exception message must not contain 'this.' artifact.");
        }

        [TestMethod]
        public void Pkcs7Padding_Unpad_WithPadLengthLargerThanBlockSize_ShouldThrowCryptographicException()
        {
            var padding = new Pkcs7Padding();

            byte[] input = new byte[BlockSize];
            for (int i = 0; i < BlockSize - 1; i++)
                input[i] = 0xAA;
            input[BlockSize - 1] = 0xFF;

            var ex = Assert.ThrowsExactly<CryptographicException>(() => padding.Unpad(input, BlockSize));
            Assert.IsFalse(ex.Message.Contains("this."), "Exception message must not contain 'this.' artifact.");
        }

        [TestMethod]
        public void Pkcs7Padding_Unpad_WithSingleBytePadding_ShouldReturnUnpaddedBytes()
        {
            // Regression guard: the constant-time geOne check must accept padLen == 1.
            // An earlier formulation of the mask rejected it and broke any message whose
            // length ended exactly one byte short of a block boundary.
            var padding = new Pkcs7Padding();

            byte[] input = new byte[BlockSize];
            for (int i = 0; i < BlockSize - 1; i++)
                input[i] = (byte)(0x30 + i);
            input[BlockSize - 1] = 0x01;

            byte[] result = padding.Unpad(input, BlockSize);
            Assert.AreEqual(BlockSize - 1, result.Length);
            for (int i = 0; i < result.Length; i++)
                Assert.AreEqual((byte)(0x30 + i), result[i]);
        }

        [TestMethod]
        public void PaddingFactory_Create_WithInvalidPaddingMode_ShouldThrowWithCleanMessage()
        {
            var ex = Assert.ThrowsExactly<CryptographicException>(() => PaddingFactory.Create((PaddingMode)999));
            Assert.IsFalse(ex.Message.Contains("this."), "Exception message must not contain 'this.' artifact.");
        }
    }
}
