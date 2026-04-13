// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensionsTests_TransformAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Bodu.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class ICryptoTransformExtensionsTests
    {
        // ---------------------------------------------------------------------------------------------------------------
        // TransformAsync(Stream, Stream, int, CancellationToken)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="transform" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_WhenTransformIsNull_ShouldThrowArgumentNullException()
        {
            ICryptoTransform? transform = null;
            using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            using var target = new MemoryStream();

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            {
                await transform!.TransformAsync(source, target, 16);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="sourceStream" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_WhenSourceStreamIsNull_ShouldThrowArgumentNullException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            using var target = new MemoryStream();

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            {
                await transform.TransformAsync(null!, target, 16);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="targetStream" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_WhenTargetStreamIsNull_ShouldThrowArgumentNullException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            {
                await transform.TransformAsync(source, null!, 16);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
        /// throws <see cref="ArgumentOutOfRangeException" /> when <paramref name="bufferSize" /> is zero.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            using var target = new MemoryStream();

            await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            {
                await transform.TransformAsync(source, target, 0);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
        /// throws <see cref="ArgumentOutOfRangeException" /> when <paramref name="bufferSize" /> is negative.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_WhenBufferSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            using var target = new MemoryStream();

            await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => {
                await transform.TransformAsync(source, target, -1);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
        /// reads all bytes from the source, applies the transform, and writes the result to the target stream.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_WhenInputIsValid_ShouldWriteTransformedBytesToTarget()
        {
            byte[] input = { 1, 2, 3, 4 };
            byte[] expected = { 4, 3, 2, 1 };

            using var source = new MemoryStream(input);
            using var target = new MemoryStream();
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            await transform.TransformAsync(source, target, bufferSize: 4);

            CollectionAssert.AreEqual(expected, target.ToArray());
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
        /// writes nothing to the target stream when the source stream is empty.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_WhenSourceIsEmpty_ShouldWriteNothingToTarget()
        {
            using var source = new MemoryStream(Array.Empty<byte>());
            using var target = new MemoryStream();
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            await transform.TransformAsync(source, target, bufferSize: 16);

            Assert.AreEqual(0, target.Length);
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
        /// does not dispose the target stream after completing the operation.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_WhenCompleted_ShouldLeaveTargetStreamOpen()
        {
            byte[] input = { 1, 2, 3, 4 };

            using var source = new MemoryStream(input);
            var target = new MemoryStream();
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            await transform.TransformAsync(source, target, bufferSize: 4);

            Assert.IsTrue(target.CanWrite, "Target stream should remain open after TransformAsync.");
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
        /// throws <see cref="OperationCanceledException" /> or <see cref="TaskCanceledException" /> when the
        /// cancellation token is signalled during a slow read operation.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_WhenCancelled_ShouldThrowCancelledException()
        {
            using var algorithm = CreateAlgorithm();
            algorithm.Padding = PaddingMode.None;

            using var source = new ThrottledIncrementingByteStream(512, readDelay: 1000);
            using var target = new MemoryStream();
            using var encryptor = algorithm.CreateEncryptor();
            using var cts = new CancellationTokenSource(millisecondsDelay: 150);

            try
            {
                await encryptor.TransformAsync(source, target, bufferSize: 32, cts.Token);
                Assert.Fail("Expected OperationCanceledException or TaskCanceledException.");
            }
            catch (OperationCanceledException)
            {
                // Expected — either OperationCanceledException or its subtype TaskCanceledException.
            }
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
        /// throws immediately when the cancellation token is already cancelled before the call.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_WhenAlreadyCancelled_ShouldThrowImmediately()
        {
            using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            using var target = new MemoryStream();
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                await transform.TransformAsync(source, target, bufferSize: 4, cts.Token);
                Assert.Fail("Expected OperationCanceledException.");
            }
            catch (OperationCanceledException)
            {
                // Expected.
            }
        }

        /// <summary>
        /// Verifies that a round-trip using <see cref="ICryptoTransformExtensions.TransformAsync" /> for
        /// encryption followed by <see cref="ICryptoTransformExtensions.TransformAsync" /> for decryption
        /// produces the original plaintext.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Stream_RoundTrip_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = System.Text.Encoding.UTF8.GetBytes("round-trip-test");

            // Encrypt
            using var sourceStream = new MemoryStream(plainText);
            using var encryptedStream = new MemoryStream();
            using (var encryptor = algorithm.CreateEncryptor())
                await encryptor.TransformAsync(sourceStream, encryptedStream, bufferSize: 32);

            // Decrypt
            encryptedStream.Position = 0;
            using var decryptedStream = new MemoryStream();
            using (var decryptor = algorithm.CreateDecryptor())
                await decryptor.TransformAsync(encryptedStream, decryptedStream, bufferSize: 32);

            CollectionAssert.AreEqual(plainText, decryptedStream.ToArray());
        }

        // ---------------------------------------------------------------------------------------------------------------
        // TransformAsync(ReadOnlyMemory<byte>, Memory<byte>, CancellationToken)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte},CancellationToken)" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="transform" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Memory_WhenTransformIsNull_ShouldThrowArgumentNullException()
        {
            ICryptoTransform? transform = null;
            var input = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 });
            var destination = new Memory<byte>(new byte[64]);

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            {
                await transform!.TransformAsync(input, destination);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte},CancellationToken)" />
        /// transforms the input memory region and writes the result into the destination, returning the byte count.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Memory_WhenInputIsValid_ShouldWriteTransformedBytesToDestination()
        {
            byte[] raw = { 1, 2, 3, 4 };
            byte[] expected = { 4, 3, 2, 1 };
            byte[] dest = new byte[64];

            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            int written = await transform.TransformAsync(
                new ReadOnlyMemory<byte>(raw),
                new Memory<byte>(dest));

            Assert.AreEqual(expected.Length, written);
            CollectionAssert.AreEqual(expected, dest[..written]);
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte},CancellationToken)" />
        /// produces output identical to the synchronous <c>Transform(ReadOnlySpan, Span)</c> overload for the same input.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Memory_WhenComparedToSyncOverload_ShouldProduceIdenticalOutput()
        {
            byte[] input = { 1, 2, 3, 4 };

            byte[] syncDest = new byte[64];
            int syncWritten;
            using (var syncTransform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest))
                syncWritten = syncTransform.Transform(input.AsSpan(), syncDest.AsSpan());

            byte[] asyncDest = new byte[64];
            int asyncWritten;
            using (var asyncTransform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest))
                asyncWritten = await asyncTransform.TransformAsync(new ReadOnlyMemory<byte>(input), new Memory<byte>(asyncDest));

            Assert.AreEqual(syncWritten, asyncWritten);
            CollectionAssert.AreEqual(syncDest[..syncWritten], asyncDest[..asyncWritten]);
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte},CancellationToken)" />
        /// throws <see cref="OperationCanceledException" /> when the token is cancelled before the operation begins.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Memory_WhenAlreadyCancelled_ShouldThrowTaskCanceledException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            var input = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 });
            var destination = new Memory<byte>(new byte[64]);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
            {
                await transform.TransformAsync(input, destination, cts.Token);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte},CancellationToken)" />
        /// throws <see cref="ArgumentException" /> when the destination buffer is too small to hold the output.
        /// </summary>
        [TestMethod]
        public async Task TransformAsync_Memory_WhenDestinationIsTooSmall_ShouldThrowArgumentException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            var input = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 });
            var tooSmall = new Memory<byte>(new byte[1]);

            await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
            {
                await transform.TransformAsync(input, tooSmall);
            });
        }
    }
}
