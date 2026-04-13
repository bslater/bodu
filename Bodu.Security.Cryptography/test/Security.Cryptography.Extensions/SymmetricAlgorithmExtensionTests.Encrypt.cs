// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensionTests_Encrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bodu.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class SymmetricAlgorithmExtensionTests
    {
        // ---------------------------------------------------------------------------------------------------------------
        // Encrypt(byte[])
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="algorithm" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArray_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            SymmetricAlgorithm? algorithm = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm!.Encrypt(new byte[] { 1, 2, 3 }));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArray_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Encrypt(null!));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[])" /> produces
        /// a non-empty result for non-empty input.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArray_WhenInputIsValid_ShouldReturnNonEmptyResult()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("encrypt-me");

            byte[] cipherText = algorithm.Encrypt(plainText);

            Assert.IsNotNull(cipherText);
            Assert.IsTrue(cipherText.Length > 0);
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[])" /> produces
        /// output that can be successfully decrypted back to the original plaintext.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArray_WhenRoundTripped_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("encrypt");

            byte[] cipherText = algorithm.Encrypt(plainText);
            byte[] decrypted = algorithm.Decrypt(cipherText);

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Encrypt(byte[], int) — from-offset overload
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[],int)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArrayOffset_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Encrypt(null!, 0));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[],int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="offset" /> is negative.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArrayOffset_WhenOffsetIsNegative_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Encrypt(data, -1));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[],int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="offset" /> exceeds the array bounds.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArrayOffset_WhenOffsetExceedsBounds_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Encrypt(data, data.Length + 1));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[],int)" /> encrypts
        /// from the given offset to the end of the array and can be decrypted back correctly.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArrayOffset_WhenOffsetIsZero_ShouldRoundTripCorrectly()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("abc");

            byte[] cipherText = algorithm.Encrypt(plainText, 0);
            byte[] decrypted = algorithm.Decrypt(cipherText);

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Encrypt(byte[], int, int)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[],int,int)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArrayRange_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Encrypt(null!, 0, 4));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[],int,int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="offset" /> is negative.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArrayRange_WhenOffsetIsNegative_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Encrypt(data, -1, 2));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[],int,int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="count" /> is negative.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArrayRange_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Encrypt(data, 0, -1));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[],int,int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the offset and count combination exceeds the array length.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArrayRange_WhenOffsetPlusCountExceedsLength_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Encrypt(data, 2, 5));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[],int,int)" />
        /// encrypts the specified range and round-trips successfully.
        /// </summary>
        [TestMethod]
        public void Encrypt_ByteArrayRange_WhenValid_ShouldRoundTripCorrectly()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("hello");

            byte[] cipherText = algorithm.Encrypt(plainText, 0, plainText.Length);
            byte[] decrypted = algorithm.Decrypt(cipherText);

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Encrypt(ReadOnlySpan<byte>)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,ReadOnlySpan{byte})" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="algorithm" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Encrypt_Span_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            SymmetricAlgorithm? algorithm = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm!.Encrypt((ReadOnlySpan<byte>)new byte[] { 1, 2, 3, 4 }));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,ReadOnlySpan{byte})" />
        /// produces a non-empty result for valid span input.
        /// </summary>
        [TestMethod]
        public void Encrypt_Span_WhenInputIsValid_ShouldReturnNonEmptyResult()
        {
            using var algorithm = CreateAlgorithm();
            ReadOnlySpan<byte> input = Encoding.UTF8.GetBytes("span-encrypt");

            byte[] cipherText = algorithm.Encrypt(input);

            Assert.IsNotNull(cipherText);
            Assert.IsTrue(cipherText.Length > 0);
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,ReadOnlySpan{byte})" />
        /// produces output that can be successfully decrypted back to the original plaintext.
        /// </summary>
        [TestMethod]
        public void Encrypt_Span_WhenRoundTripped_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("span-round-trip");

            byte[] cipherText = algorithm.Encrypt((ReadOnlySpan<byte>)plainText);
            byte[] decrypted = algorithm.Decrypt(cipherText);

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,ReadOnlySpan{byte})" />
        /// produces output identical to the byte-array overload for the same input.
        /// </summary>
        [TestMethod]
        public void Encrypt_Span_WhenComparedToByteArrayOverload_ShouldProduceIdenticalOutput()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("identical-output");

            byte[] fromArray = algorithm.Encrypt(plainText);
            byte[] fromSpan = algorithm.Encrypt((ReadOnlySpan<byte>)plainText);

            CollectionAssert.AreEqual(fromArray, fromSpan);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Encrypt(ReadOnlyMemory<byte>)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,ReadOnlyMemory{byte})" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="algorithm" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Encrypt_Memory_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            SymmetricAlgorithm? algorithm = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm!.Encrypt(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 })));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,ReadOnlyMemory{byte})" />
        /// produces output that can be successfully decrypted back to the original plaintext.
        /// </summary>
        [TestMethod]
        public void Encrypt_Memory_WhenRoundTripped_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("memory-encrypt");

            byte[] cipherText = algorithm.Encrypt(new ReadOnlyMemory<byte>(plainText));
            byte[] decrypted = algorithm.Decrypt(cipherText);

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,ReadOnlyMemory{byte})" />
        /// produces output identical to the span overload for the same input.
        /// </summary>
        [TestMethod]
        public void Encrypt_Memory_WhenComparedToSpanOverload_ShouldProduceIdenticalOutput()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("identical-memory");

            byte[] fromSpan = algorithm.Encrypt((ReadOnlySpan<byte>)plainText);
            byte[] fromMemory = algorithm.Encrypt(new ReadOnlyMemory<byte>(plainText));

            CollectionAssert.AreEqual(fromSpan, fromMemory);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Encrypt(Stream, Stream) — default buffer size overload
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,Stream,Stream)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="algorithm" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Encrypt_Stream_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            SymmetricAlgorithm? algorithm = null;
            using var source = new MemoryStream();
            using var target = new MemoryStream();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm!.Encrypt(source, target));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,Stream,Stream)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="sourceStream" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Encrypt_Stream_WhenSourceStreamIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            using var target = new MemoryStream();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Encrypt(null!, target));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,Stream,Stream)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="targetStream" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Encrypt_Stream_WhenTargetStreamIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            using var source = new MemoryStream();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Encrypt(source, null!));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,Stream,Stream)" />
        /// encrypts the stream content and the result can be decrypted back to the original plaintext.
        /// </summary>
        [TestMethod]
        public void Encrypt_Stream_WhenRoundTripped_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("stream-encrypt-default-buffer");

            byte[] cipherText = algorithm.Encrypt(plainText);

            using var cipherStream = new MemoryStream(cipherText);
            using var decryptedStream = new MemoryStream();
            algorithm.Decrypt(cipherStream, decryptedStream);

            CollectionAssert.AreEqual(plainText, decryptedStream.ToArray());
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Encrypt(Stream, Stream, int) — explicit buffer size overload
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,Stream,Stream,int)" />
        /// throws <see cref="ArgumentOutOfRangeException" /> when <paramref name="bufferSize" /> is negative.
        /// </summary>
        [TestMethod]
        public void Encrypt_StreamWithBufferSize_WhenBufferSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            using var source = new MemoryStream();
            using var target = new MemoryStream();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Encrypt(source, target, -128));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,Stream,Stream,int)" />
        /// throws <see cref="ArgumentOutOfRangeException" /> when <paramref name="bufferSize" /> is zero.
        /// </summary>
        [TestMethod]
        public void Encrypt_StreamWithBufferSize_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            using var source = new MemoryStream();
            using var target = new MemoryStream();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Encrypt(source, target, 0));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,Stream,Stream,int)" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="targetStream" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Encrypt_StreamWithBufferSize_WhenTargetStreamIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            using var source = new MemoryStream();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Encrypt(source, null!, 1024));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,Stream,Stream,int)" />
        /// encrypts stream data correctly and round-trips to the original plaintext.
        /// </summary>
        [TestMethod]
        public void Encrypt_StreamWithBufferSize_WhenRoundTripped_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("stream-encrypt-explicit-buffer");

            using var sourceStream = new MemoryStream(plainText);
            using var encryptedStream = new MemoryStream();
            algorithm.Encrypt(sourceStream, encryptedStream, bufferSize: 64);

            encryptedStream.Position = 0;
            using var decryptedStream = new MemoryStream();
            algorithm.Decrypt(encryptedStream, decryptedStream);

            CollectionAssert.AreEqual(plainText, decryptedStream.ToArray());
        }
    }
}
