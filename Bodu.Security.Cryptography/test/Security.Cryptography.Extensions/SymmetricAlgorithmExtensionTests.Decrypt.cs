// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensionTests_Decrypt.cs" company="PlaceholderCompany">
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
        // Decrypt(byte[])
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="algorithm" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArray_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            SymmetricAlgorithm? algorithm = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm!.Decrypt(new byte[] { 1, 2, 3, 4 }));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArray_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Decrypt(null!));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[])" /> correctly
        /// decrypts ciphertext produced by <see cref="SymmetricAlgorithmExtensions.Encrypt(SymmetricAlgorithm,byte[])" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArray_WhenRoundTripped_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("abc");

            byte[] cipherText = algorithm.Encrypt(plainText);
            byte[] decrypted = algorithm.Decrypt(cipherText);

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Decrypt(byte[], int) — from-offset overload
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[],int)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArrayOffset_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Decrypt(null!, 0));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[],int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="offset" /> exceeds the array bounds.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArrayOffset_WhenOffsetExceedsBounds_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = new byte[10];

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Decrypt(data, 20));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[],int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="offset" /> is negative.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArrayOffset_WhenOffsetIsNegative_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Decrypt(data, -1));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[],int)" />
        /// correctly decrypts from offset zero to the end of the array.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArrayOffset_WhenOffsetIsZero_ShouldRoundTripCorrectly()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("abc");

            byte[] cipherText = algorithm.Encrypt(plainText);
            byte[] decrypted = algorithm.Decrypt(cipherText, 0);

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Decrypt(byte[], int, int)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[],int,int)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArrayRange_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Decrypt(null!, 0, 4));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[],int,int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="offset" /> is negative.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArrayRange_WhenOffsetIsNegative_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Decrypt(data, -1, 2));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[],int,int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="count" /> is negative.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArrayRange_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Decrypt(data, 0, -1));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[],int,int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when the offset and count combination exceeds the array length.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArrayRange_WhenOffsetPlusCountExceedsLength_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Decrypt(data, 2, 5));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,byte[],int,int)" />
        /// correctly decrypts the specified range and round-trips to the original plaintext.
        /// </summary>
        [TestMethod]
        public void Decrypt_ByteArrayRange_WhenValid_ShouldRoundTripCorrectly()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("abcde");

            byte[] cipherText = algorithm.Encrypt(plainText);
            byte[] decrypted = algorithm.Decrypt(cipherText, 0, cipherText.Length);

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Decrypt(ReadOnlySpan<byte>)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,ReadOnlySpan{byte})" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="algorithm" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_Span_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            SymmetricAlgorithm? algorithm = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm!.Decrypt((ReadOnlySpan<byte>)new byte[] { 1, 2, 3, 4 }));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,ReadOnlySpan{byte})" />
        /// correctly decrypts span-wrapped ciphertext back to the original plaintext.
        /// </summary>
        [TestMethod]
        public void Decrypt_Span_WhenRoundTripped_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("span-decrypt");

            byte[] cipherText = algorithm.Encrypt(plainText);
            byte[] decrypted = algorithm.Decrypt((ReadOnlySpan<byte>)cipherText);

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,ReadOnlySpan{byte})" />
        /// produces output identical to the byte-array overload for the same ciphertext input.
        /// </summary>
        [TestMethod]
        public void Decrypt_Span_WhenComparedToByteArrayOverload_ShouldProduceIdenticalOutput()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("identical-decrypt");
            byte[] cipherText = algorithm.Encrypt(plainText);

            byte[] fromArray = algorithm.Decrypt(cipherText);
            byte[] fromSpan = algorithm.Decrypt((ReadOnlySpan<byte>)cipherText);

            CollectionAssert.AreEqual(fromArray, fromSpan);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Decrypt(ReadOnlyMemory<byte>)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,ReadOnlyMemory{byte})" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="algorithm" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_Memory_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            SymmetricAlgorithm? algorithm = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm!.Decrypt(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 })));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,ReadOnlyMemory{byte})" />
        /// correctly decrypts memory-wrapped ciphertext back to the original plaintext.
        /// </summary>
        [TestMethod]
        public void Decrypt_Memory_WhenRoundTripped_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("memory-decrypt");

            byte[] cipherText = algorithm.Encrypt(plainText);
            byte[] decrypted = algorithm.Decrypt(new ReadOnlyMemory<byte>(cipherText));

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,ReadOnlyMemory{byte})" />
        /// produces output identical to the span overload for the same ciphertext input.
        /// </summary>
        [TestMethod]
        public void Decrypt_Memory_WhenComparedToSpanOverload_ShouldProduceIdenticalOutput()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("memory-vs-span");
            byte[] cipherText = algorithm.Encrypt(plainText);

            byte[] fromSpan = algorithm.Decrypt((ReadOnlySpan<byte>)cipherText);
            byte[] fromMemory = algorithm.Decrypt(new ReadOnlyMemory<byte>(cipherText));

            CollectionAssert.AreEqual(fromSpan, fromMemory);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Decrypt(Stream, Stream) — default buffer size overload
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,Stream,Stream)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="algorithm" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_Stream_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            SymmetricAlgorithm? algorithm = null;
            using var source = new MemoryStream();
            using var target = new MemoryStream();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm!.Decrypt(source, target));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,Stream,Stream)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="sourceStream" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_Stream_WhenSourceStreamIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            using var target = new MemoryStream();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Decrypt(null!, target));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,Stream,Stream)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="targetStream" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_Stream_WhenTargetStreamIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            using var source = new MemoryStream();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Decrypt(source, null!));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,Stream,Stream)" />
        /// correctly decrypts stream content and round-trips to the original plaintext.
        /// </summary>
        [TestMethod]
        public void Decrypt_Stream_WhenRoundTripped_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("stream-data");

            byte[] cipherText = algorithm.Encrypt(plainText);
            using var source = new MemoryStream(cipherText);
            using var target = new MemoryStream();

            algorithm.Decrypt(source, target);

            CollectionAssert.AreEqual(plainText, target.ToArray());
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Decrypt(Stream, Stream, int) — explicit buffer size overload
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,Stream,Stream,int)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="sourceStream" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_StreamWithBufferSize_WhenSourceStreamIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            using var target = new MemoryStream();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm.Decrypt(null!, target, 1024));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,Stream,Stream,int)" /> throws
        /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="bufferSize" /> is zero.
        /// </summary>
        [TestMethod]
        public void Decrypt_StreamWithBufferSize_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
        {
            using var algorithm = CreateAlgorithm();
            using var source = new MemoryStream();
            using var target = new MemoryStream();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                algorithm.Decrypt(source, target, 0));
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithmExtensions.Decrypt(SymmetricAlgorithm,Stream,Stream,int)" />
        /// correctly decrypts stream data and round-trips to the original plaintext.
        /// </summary>
        [TestMethod]
        public void Decrypt_StreamWithBufferSize_WhenRoundTripped_ShouldProduceOriginalPlaintext()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("explicit-buffer-decrypt");

            byte[] cipherText = algorithm.Encrypt(plainText);
            using var source = new MemoryStream(cipherText);
            using var target = new MemoryStream();

            algorithm.Decrypt(source, target, bufferSize: 64);

            CollectionAssert.AreEqual(plainText, target.ToArray());
        }
    }
}
