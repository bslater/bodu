// -----------------------------------------------------------------------
// <copyright file="BKDR.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Bodu.Infrastructure;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Extensions
{
	public partial class SymmetricAlgorithmExtensionTests
	{
		[TestMethod]
		[DataRow(PaddingMode.None, true)]
		[DataRow(PaddingMode.PKCS7, false)]
		[DataRow(PaddingMode.ANSIX923, false)]
		[DataRow(PaddingMode.ISO10126, false)]
		[DataRow(PaddingMode.Zeros, true)]
		public async Task DecryptAsync_EmptyInput_WithVariousPaddingModes_ShouldBehaveCorrectly(PaddingMode padding, bool expectSuccess)
		{
			using var algorithm = CreateAlgorithm();
			algorithm.Padding = padding;

			using var input = new MemoryStream(Array.Empty<byte>());
			var output = new MemoryStream();

			try
			{
				if (expectSuccess)
				{
					await algorithm.DecryptAsync(input, output);
					Assert.AreEqual(0, output.Length);
				}
				else
				{
					await Assert.ThrowsExactlyAsync<CryptographicException>(() =>
					{
						return algorithm.DecryptAsync(input, output);
					});
				}
			}
			finally
			{
				output.Dispose(); // Manual cleanup
			}
		}

		[TestMethod]
		public async Task DecryptAsync_WhenBufferSizeIsZero_ShouldThrow()
		{
			using var algorithm = CreateAlgorithm();
			using var input = new MemoryStream();
			using var output = new MemoryStream();

			await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
			{
				await algorithm.DecryptAsync(input, output, 0);
			});
		}

		[TestMethod]
		public async Task DecryptAsync_WhenCalled_WithNoPadding_ShouldNotDisposeTargetStream()
		{
			using var algorithm = CreateAlgorithm();
			algorithm.Padding = PaddingMode.None;

			using var input = new MemoryStream(); // empty input
			using var output = new MemoryStream();

			await algorithm.DecryptAsync(input, output);

			// The output stream should still be open and writable
			Assert.IsTrue(output.CanWrite, "Output stream should remain open after DecryptAsync.");

			// Optionally, verify we can still write to the stream
			try
			{
				output.WriteByte(0xFF); // Write additional byte
			}
			catch (ObjectDisposedException)
			{
				Assert.Fail("Output stream was unexpectedly closed or disposed.");
			}
		}

        [TestMethod]
        public async Task DecryptAsync_WhenCancelled_ShouldThrowTaskCanceledException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Enumerable.Range(0, 512).Select(i => (byte)i).ToArray();
            byte[] encrypted = algorithm.Encrypt(plainText);

            // Valid ciphertext in a plain MemoryStream — no cryptographic errors.
            using var input = new MemoryStream(encrypted);

            // Throttle the output writes so the cancellation deadline fires first.
            using var output = new ThrottledOutputMemoryStream(delayMilliseconds: 1000);
            using var cts = new CancellationTokenSource(millisecondsDelay: 100);

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(() =>
			{
				return algorithm.DecryptAsync(input, output, 64, cts.Token);
			});
        }

        [TestMethod]
		public async Task DecryptAsync_WhenOutputIsStreamNull_ShouldThrow()
		{
			using var algorithm = CreateAlgorithm();
			using var input = new MemoryStream(Enumerable.Range(0, algorithm.BlockSize / 8).Select(b => (byte)b).ToArray());

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
			{
				return algorithm.DecryptAsync(input, null, CancellationToken.None);
			});
		}

        [TestMethod]
        public async Task DecryptAsync_WhenOutputIsThrottled_ShouldStillProduceResult()
        {
            using var algorithm = CreateAlgorithm();
            byte[] original = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();
            byte[] encrypted = algorithm.Encrypt(original);

            using var input = new MemoryStream(encrypted);
            using var throttledOutput = new ThrottledOutputMemoryStream(delayMilliseconds: 50);

            await algorithm.DecryptAsync(input, throttledOutput, 32);

            CollectionAssert.AreEqual(original, throttledOutput.ToArray());
        }

        [TestMethod]
		public async Task DecryptAsync_WhenSourceStreamIsNull_ShouldThrow()
		{
			using var algorithm = CreateAlgorithm();
			using var output = new MemoryStream();

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
			{
				await algorithm.DecryptAsync(null!, output);
			});
		}

		[TestMethod]
		public async Task DecryptAsync_WithThrottledInput_ShouldProduceCorrectResult()
		{
			using var algorithm = CreateAlgorithm();
			byte[] plainText = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
			byte[] encrypted = algorithm.Encrypt(plainText);

			using var input = new MemoryStream(encrypted);
			using var output = new MemoryStream();

			await algorithm.DecryptAsync(input, output, bufferSize: 16);
			byte[] decrypted = output.ToArray();

			CollectionAssert.AreEqual(plainText, decrypted);
		}
	}
}