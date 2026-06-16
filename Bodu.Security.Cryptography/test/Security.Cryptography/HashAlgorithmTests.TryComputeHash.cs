// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.TryComputeHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
    {
        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" />
        /// returns <see langword="false" /> and writes zero bytes when the destination is too small.
        /// </summary>
        [TestMethod]
        public void TryComputeHash_WhenDestinationTooSmall_ShouldReturnFalseAndWriteZeroBytes()
        {
            byte[] input = CryptoTestUtilities.ByteSequence256;

            using TAlgorithm expectedAlgorithm = CreateAlgorithm();
            byte[] expected = expectedAlgorithm.ComputeHash(input);

            using TAlgorithm algorithm = CreateAlgorithm();
            byte[] destination = new byte[expected.Length - 1];

            bool result = algorithm.TryComputeHash(input, destination, out int bytesWritten);

            Assert.IsFalse(result);
            Assert.AreEqual(0, bytesWritten);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" />
        /// returns <see langword="true" /> and writes the expected hash when the destination is exactly the hash size.
        /// </summary>
        [TestMethod]
        public void TryComputeHash_WhenDestinationIsExactSize_ShouldReturnTrueAndWriteHash()
        {
            byte[] input = CryptoTestUtilities.ByteSequence256;

            using TAlgorithm expectedAlgorithm = CreateAlgorithm();
            byte[] expected = expectedAlgorithm.ComputeHash(input);

            using TAlgorithm algorithm = CreateAlgorithm();
            byte[] destination = new byte[expected.Length];

            bool result = algorithm.TryComputeHash(input, destination, out int bytesWritten);

            Assert.IsTrue(result);
            Assert.AreEqual(expected.Length, bytesWritten);
            CollectionAssert.AreEqual(expected, destination);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" />
        /// does not modify bytes beyond the written hash when the destination is larger than required.
        /// </summary>
        [TestMethod]
        public void TryComputeHash_WhenDestinationIsLargerThanHash_ShouldNotModifyTrailingBytes()
        {
            byte[] input = CryptoTestUtilities.ByteSequence256;

            using TAlgorithm expectedAlgorithm = CreateAlgorithm();
            byte[] expected = expectedAlgorithm.ComputeHash(input);

            using TAlgorithm algorithm = CreateAlgorithm();
            byte[] destination = new byte[expected.Length + 1];
            destination[^1] = 0xA5;

            bool result = algorithm.TryComputeHash(input, destination, out int bytesWritten);

            Assert.IsTrue(result);
            Assert.AreEqual(expected.Length, bytesWritten);
            CollectionAssert.AreEqual(expected, destination.AsSpan(0, expected.Length).ToArray());
            Assert.AreEqual(0xA5, destination[^1]);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" />
        /// clears the public <see cref="HashAlgorithm.Hash" /> value after a previous
        /// <see cref="HashAlgorithm.ComputeHash(byte[])" /> operation populated it.
        /// </summary>
        [TestMethod]
        public void TryComputeHash_WhenCalledAfterComputeHash_ShouldClearHashProperty()
        {
            byte[] input = CryptoTestUtilities.ByteSequence256;

            using TAlgorithm algorithm = CreateAlgorithm();
            if (!algorithm.CanReuseTransform)
            {
                Assert.Inconclusive($"{typeof(TAlgorithm).Name} reports CanReuseTransform=false; the same instance cannot serve two hash computations.");
                return;
            }

            byte[] hash = algorithm.ComputeHash(input);
            Assert.IsNotNull(algorithm.Hash);

            bool result = algorithm.TryComputeHash(input, hash, out int bytesWritten);

            Assert.IsTrue(result);
            Assert.AreEqual(hash.Length, bytesWritten);
            Assert.IsNull(algorithm.Hash);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" />
        /// throws an <see cref="ObjectDisposedException" /> when the algorithm has been disposed.
        /// </summary>
        [TestMethod]
        public void TryComputeHash_WhenDisposed_ShouldThrowExactly()
        {
            TAlgorithm algorithm = CreateAlgorithm();
            algorithm.Dispose();

            byte[] input = new byte[1];
            byte[] destination = new byte[Math.Max(1, algorithm.HashSize / 8)];

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                _ = algorithm.TryComputeHash(input, destination, out _);
            });
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" />
        /// succeeds when the source and destination begin at the same position in the same buffer.
        /// </summary>
        [TestMethod]
        public void TryComputeHash_WhenSourceAndDestinationAreSameBuffer_ShouldSucceed()
        {
            using TAlgorithm expectedAlgorithm = CreateAlgorithm();
            int hashLength = expectedAlgorithm.HashSize / 8;

            byte[] expectedInput = Enumerable.Range(0, hashLength)
                .Select(i => (byte)i)
                .ToArray();

            byte[] expected = expectedAlgorithm.ComputeHash(expectedInput);

            byte[] buffer = expectedInput.ToArray();

            using TAlgorithm algorithm = CreateAlgorithm();

            bool result = algorithm.TryComputeHash(
                buffer.AsSpan(0, hashLength),
                buffer.AsSpan(0, hashLength),
                out int bytesWritten);

            Assert.IsTrue(result);
            Assert.AreEqual(hashLength, bytesWritten);
            CollectionAssert.AreEqual(expected, buffer);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" />
        /// succeeds when the destination overlaps the source at a later position in the same buffer.
        /// </summary>
        [TestMethod]
        public void TryComputeHash_WhenDestinationOverlapsSourceForward_ShouldSucceed()
        {
            const int destinationOffset = 4;

            using TAlgorithm expectedAlgorithm = CreateAlgorithm();
            int hashLength = expectedAlgorithm.HashSize / 8;
            int inputLength = hashLength + 16;

            byte[] expectedInput = Enumerable.Range(0, inputLength)
                .Select(i => (byte)i)
                .ToArray();

            byte[] expected = expectedAlgorithm.ComputeHash(expectedInput);

            byte[] buffer = new byte[inputLength + destinationOffset + hashLength];
            Buffer.BlockCopy(expectedInput, 0, buffer, 0, expectedInput.Length);

            using TAlgorithm algorithm = CreateAlgorithm();

            bool result = algorithm.TryComputeHash(
                buffer.AsSpan(0, inputLength),
                buffer.AsSpan(destinationOffset, hashLength),
                out int bytesWritten);

            Assert.IsTrue(result);
            Assert.AreEqual(hashLength, bytesWritten);
            CollectionAssert.AreEqual(expected, buffer.AsSpan(destinationOffset, hashLength).ToArray());
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" />
        /// succeeds when the destination overlaps the source at an earlier position in the same buffer.
        /// </summary>
        [TestMethod]
        public void TryComputeHash_WhenDestinationOverlapsSourceBackward_ShouldSucceed()
        {
            const int sourceOffset = 4;

            using TAlgorithm expectedAlgorithm = CreateAlgorithm();
            int hashLength = expectedAlgorithm.HashSize / 8;
            int inputLength = hashLength + 16;

            byte[] expectedInput = Enumerable.Range(0, inputLength)
                .Select(i => (byte)i)
                .ToArray();

            byte[] expected = expectedAlgorithm.ComputeHash(expectedInput);

            byte[] buffer = new byte[sourceOffset + inputLength];
            Buffer.BlockCopy(expectedInput, 0, buffer, sourceOffset, expectedInput.Length);

            using TAlgorithm algorithm = CreateAlgorithm();

            bool result = algorithm.TryComputeHash(
                buffer.AsSpan(sourceOffset, inputLength),
                buffer.AsSpan(0, hashLength),
                out int bytesWritten);

            Assert.IsTrue(result);
            Assert.AreEqual(hashLength, bytesWritten);
            CollectionAssert.AreEqual(expected, buffer.AsSpan(0, hashLength).ToArray());
        }
    }
}