using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Security.Cryptography;
using Bodu.Testing.Security;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
	public abstract partial class BlockCipherModeTests<TMode>
	{
		[TestMethod]
		public void Transform_WhenInputNotBlockAligned_ShouldThrow()
		{
			var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
			var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
			var transform = CreateTransform(cipher, iv);

			var input = new byte[ExpectedBlockSize + 1];
			var output = new byte[input.Length];

			Assert.ThrowsExactly<ArgumentException>(() =>
			{
				transform.Transform(input, output, encrypt: true);
			});
		}

		[TestMethod]
		public void Transform_WhenOutputTooSmall_ShouldThrow()
		{
			var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
			var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
			var transform = CreateTransform(cipher, iv);

			var input = new byte[ExpectedBlockSize];
			var output = new byte[ExpectedBlockSize - 1];

			Assert.ThrowsExactly<ArgumentException>(() =>
			{
				transform.Transform(input, output, encrypt: true);
			});
		}

		[TestMethod]
		public void Transform_WhenRoundTripped_ShouldReturnOriginal()
		{
			var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
			var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);

			var transformEncrypt = CreateTransform(cipher, (byte[])iv.Clone());
			var transformDecrypt = CreateTransform(cipher, (byte[])iv.Clone());

			var plaintext = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize * 2);
			var ciphertext = new byte[plaintext.Length];
			var decrypted = new byte[plaintext.Length];

			transformEncrypt.Transform(plaintext, ciphertext, encrypt: true);
			transformDecrypt.Transform(ciphertext, decrypted, encrypt: false);

			CollectionAssert.AreEqual(plaintext, decrypted);
		}

		[TestMethod]
		public void Transform_WithAllZeroInput_ShouldProcessCorrectly()
		{
			var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
			var iv = Enumerable.Repeat((byte)0x00, ExpectedBlockSize).ToArray();
			var transform = CreateTransform(cipher, iv);

			var input = new byte[ExpectedBlockSize * 2];
			var output = new byte[input.Length];

			transform.Transform(input, output, encrypt: true);

			Assert.IsTrue(output.All(b => b == 0));
		}

		[TestMethod]
		public void Transform_WithAlternatingBitsAA55_ShouldSucceed()
		{
			var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
			var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
			var transform = CreateTransform(cipher, iv);

			var input = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)(i % 2 == 0 ? 0xAA : 0x55)).ToArray();
			var output = new byte[input.Length];

			transform.Transform(input, output, encrypt: true);

			Assert.IsTrue(output.Any(b => b != 0));
		}

		[TestMethod]
		public void Transform_WithMaxValueInput_ShouldSucceed()
		{
			var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
			var iv = Enumerable.Repeat((byte)0xFF, ExpectedBlockSize).ToArray();
			var transform = CreateTransform(cipher, iv);

			var input = Enumerable.Repeat((byte)0xFF, ExpectedBlockSize * 2).ToArray();
			var output = new byte[input.Length];

			transform.Transform(input, output, encrypt: true);

			Assert.IsTrue(output.All(b => b != 0));
		}

		[TestMethod]
		public void Transform_WithMirroredPattern_ShouldSucceed()
		{
			var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
			var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
			var transform = CreateTransform(cipher, iv);

			var input = Enumerable.Range(0, ExpectedBlockSize * 2)
				.Select(i => (byte)(i < ExpectedBlockSize ? i : ExpectedBlockSize * 2 - i - 1))
				.ToArray();
			var output = new byte[input.Length];

			transform.Transform(input, output, encrypt: true);

			Assert.IsTrue(output.Any(b => b != 0));
		}

		[TestMethod]
		public void Transform_WithNibblePatternF00F_ShouldSucceed()
		{
			var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
			var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
			var transform = CreateTransform(cipher, iv);

			var input = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)(i % 2 == 0 ? 0xF0 : 0x0F)).ToArray();
			var output = new byte[input.Length];

			transform.Transform(input, output, encrypt: true);

			Assert.IsTrue(output.Any(b => b != 0));
		}

		[TestMethod]
		public void Transform_WithRepeatingPatternInput_ShouldProcessEachBlockIndependently()
		{
			if (this is EcbModeTransformTests)
				Assert.Inconclusive("ECB mode does not alter repeating blocks.");

			var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xFF);
			var iv = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)i).ToArray();
			var transform = CreateTransform(cipher, (byte[])iv.Clone());

			var block = Enumerable.Repeat((byte)0xAA, ExpectedBlockSize).ToArray();
			var input = block.Concat(block).ToArray();
			var output = new byte[input.Length];

			transform.Transform(input, output, encrypt: true);

			Assert.AreNotEqual(BitConverter.ToString(output[..ExpectedBlockSize]), BitConverter.ToString(output[ExpectedBlockSize..]));
		}

		[TestMethod]
		public void Transform_WithSawtoothPattern_ShouldSucceed()
		{
			var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
			var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
			var transform = CreateTransform(cipher, iv);

			var input = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)(i % 16)).ToArray();
			var output = new byte[input.Length];

			transform.Transform(input, output, encrypt: true);

			Assert.IsTrue(output.Any(b => b != 0));
		}

        /// <summary>
        /// Verifies that a mode transform that guards against keystream reuse throws
        /// <see cref="CryptographicException" /> when the counter would wrap back to its
        /// initial value. Test uses a 1-byte block cipher so the wrap occurs after 256 calls.
        /// </summary>
        [TestMethod]
        public void Transform_WhenCounterWouldWrap_ShouldThrowCryptographicException()
        {
            if (!this.GuardsAgainstKeystreamReuse)
            {
                Assert.Inconclusive($"{typeof(TMode).Name} does not guard against keystream reuse.");
                return;
            }

            var cipher = new MonitoringBlockCipher(1);
            var transform = this.CreateTransform(cipher, new byte[1] { 0 });

            byte[] inputBlock = new byte[1];
            byte[] outputBlock = new byte[1];

            for (int i = 0; i < 256; i++)
            {
                transform.Transform(inputBlock, outputBlock, encrypt: true);
            }

            Assert.ThrowsExactly<CryptographicException>(
                () => transform.Transform(inputBlock, outputBlock, encrypt: true));
        }
    }
}