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
        public async Task EncryptAsync_WhenBufferSizeIsNegative_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var input = new MemoryStream();
            using var output = new MemoryStream();

            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
            {
                await algorithm.EncryptAsync(input, output, -1);
            });
        }

        [TestMethod]
        public async Task EncryptAsync_WhenCancelled_ShouldThrowOperationCanceledException()
        {
            using var algorithm = CreateAlgorithm();
            var input = new ThrottledIncrementingByteStream(512);
            using var output = new ThrottledOutputMemoryStream();
            using var cts = new CancellationTokenSource(millisecondsDelay: 250);

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            {
                await algorithm.EncryptAsync(input, output, bufferSize: 32, cts.Token);
            });
        }

        [TestMethod]
        public async Task EncryptAsync_WhenCancelled_ShouldThrowTaskCanceledException()
        {
            using var algorithm = CreateAlgorithm();
            using var input = new ThrottledIncrementingByteStream(128); // 128 bytes
            using var output = new MemoryStream();
            using var cts = new CancellationTokenSource(millisecondsDelay: 100); // cancel before read completes

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
                algorithm.EncryptAsync(input, output, 64, cts.Token));
        }

        [TestMethod]
        public async Task EncryptAsync_WhenInputIsEmptyStream_ShouldProduceEmptyOutput()
        {
            using var algorithm = CreateAlgorithm();
            using var input = new MemoryStream(Array.Empty<byte>());
            using var output = new MemoryStream();

            await algorithm.EncryptAsync(input, output);
            Assert.AreEqual(0, output.Length);
        }

        [TestMethod]
        public async Task EncryptAsync_WhenOutputIsStreamNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var input = new MemoryStream(new byte[] { 1, 2, 3 });

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                algorithm.EncryptAsync(input, Stream.Null, CancellationToken.None));
        }

        [TestMethod]
        public async Task EncryptAsync_WhenOutputIsThrottled_ShouldStillProduceResult()
        {
            using var algorithm = CreateAlgorithm();
            byte[] inputBytes = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();

            using var input = new MemoryStream(inputBytes);
            using var throttledOutput = new ThrottledOutputMemoryStream();

            await algorithm.EncryptAsync(input, throttledOutput, 32);
            throttledOutput.Position = 0;

            byte[] decrypted = algorithm.Decrypt(throttledOutput.ToArray());
            CollectionAssert.AreEqual(inputBytes, decrypted);
        }

        [TestMethod]
        public async Task EncryptAsync_WhenTargetStreamIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var input = new MemoryStream();

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.EncryptAsync(input, null!);
            });
        }

        [TestMethod]
        public async Task EncryptAsync_WhenUsingThrottledStreams_ShouldSucceed()
        {
            using var algorithm = CreateAlgorithm();
            var input = new ThrottledIncrementingByteStream(256);
            var expected = input.ToArray();

            using var output = new ThrottledOutputMemoryStream();
            await algorithm.EncryptAsync(input, output, bufferSize: 64, CancellationToken.None);

            byte[] cipher = output.ToArray();
            using var cipherStream = new MemoryStream(cipher);
            using var decrypted = new MemoryStream();

            await algorithm.DecryptAsync(cipherStream, decrypted, bufferSize: 64, CancellationToken.None);

            CollectionAssert.AreEqual(expected, decrypted.ToArray());
        }

        [TestMethod]
        public async Task EncryptAsync_WithThrottledInput_ShouldProduceCorrectResult()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();

            using var input = new IncrementingByteStream(64);
            using var output = new MemoryStream();

            await algorithm.EncryptAsync(input, output, bufferSize: 16);
            byte[] cipher = output.ToArray();
            byte[] decrypted = algorithm.Decrypt(cipher);

            CollectionAssert.AreEqual(plainText, decrypted);
        }
    }
}