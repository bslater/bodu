// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTestUtility.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides shared constants and helpers for cryptographic test cases.
    /// </summary>
    internal static partial class CryptoTestUtilities
    {
        /// <summary>A single zero byte.</summary>
        public static readonly byte[] EmptyByteArray = Array.Empty<byte>();

        /// <summary>Sequential bytes 0x00–0x0F (16 bytes) — 128-bit block size.</summary>
        public static readonly byte[] ByteSequence16 = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();

        /// <summary>Sequential bytes 0x00–0x1F (32 bytes) — 256-bit block size.</summary>
        public static readonly byte[] ByteSequence32 = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

        /// <summary>Sequential bytes 0x00–0x3F (64 bytes) — 512-bit block size.</summary>
        public static readonly byte[] ByteSequence64 = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();

        /// <summary>Sequential bytes 0x00–0x7F (128 bytes) — 1024-bit block size (Threefish-1024).</summary>
        public static readonly byte[] ByteSequence128 = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();

        /// <summary>Sequential bytes 0x00–0xFF (256 bytes) — 2048-bit block size.</summary>
        public static readonly byte[] ByteSequence256 = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        /// <summary>
        /// Generates a deterministic byte sequence of the specified length using the
        /// <c>(i * 31) + 7</c> pattern, matching the entropy test convention used throughout the suite.
        /// </summary>
        public static byte[] CreateDeterministicBytes(int length) =>
            Enumerable.Range(0, length).Select(i => (byte)((i * 31) + 7)).ToArray();

        /// <summary>The ASCII text used as a common short-message test input across the suite.</summary>
        public const string SimpleText = "The quick brown fox jumps over the lazy dog";

        /// <summary>The ASCII-encoded byte representation of <see cref="SimpleText" />.</summary>
        public static readonly byte[] SimpleTextAsciiBytes = Encoding.ASCII.GetBytes(SimpleText);

        /// <summary>
        /// Returns the number of bits in the byte array.
        /// </summary>
        /// <param name="data">The byte array to evaluate.</param>
        /// <returns>The number of bits (i.e. <c>data.Length * 8</c>).</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="data" /> is <see langword="null" />.</exception>
        public static int ToBitLength(this byte[] data)
        {
            ThrowHelper.ThrowIfNull(data);

            return data.Length * 8;
        }

        /// <summary>
        /// Creates a cryptographically random byte array whose elements are all non-zero.
        /// </summary>
        /// <param name="count">/// The number of bytes to generate./// </param>
        /// <returns>
        /// A byte array containing <paramref name="count" /> cryptographically random non-zero bytes.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count" /> is less than zero.
        /// </exception>
        public static byte[] GetRandomNonZeroBytes(int count)
        {
            ThrowHelper.ThrowIfNegative(count);

            byte[] buffer = new byte[count];
            CryptoHelpers.FillWithRandomNonZeroBytes(buffer);
            return buffer;
        }

        /// <summary>
        /// Creates a deterministic sequence of incrementing byte values.
        /// </summary>
        /// <param name="start">
        /// The first byte value in the sequence.
        /// </param>
        /// <param name="count">
        /// The number of bytes to generate.
        /// </param>
        /// <returns>
        /// A byte array containing <paramref name="count" /> incrementing values starting at <paramref name="start" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count" /> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The requested sequence exceeds the range of <see cref="byte" />.
        /// </exception>
        public static byte[] CreateIncrementalByteSequence(byte start, int count)
        {
            ThrowHelper.ThrowIfLessThan(start, 0);
            ThrowHelper.ThrowIfNegative(count);

            if (count > (byte.MaxValue - start + 1))
            {
                throw new ArgumentException(
                    "The requested byte sequence exceeds the valid byte range.",
                    nameof(count));
            }

            return Enumerable.Range(start, count)
                             .Select(static i => (byte)i)
                             .ToArray();
        }

        /// <summary>
        /// Returns a key whose bit length falls outside all ranges in <paramref name="legalSizes" />,
        /// or <see langword="null" /> when every byte-aligned length from 1 to 512 bytes is legal.
        /// </summary>
        public static byte[]? FindInvalidKey(KeySizes[] legalSizes)
        {
            // Try byte-aligned lengths from 1 up to a generous ceiling.
            // The first length (in bits) that is not covered by any legal range becomes the invalid key.
            for (int bytes = 1; bytes <= 512; bytes++)
            {
                int bits = bytes * 8;
                if (!IsLegalKeySize(bits, legalSizes))
                    return new byte[bytes];
            }

            return null; // every byte-aligned length up to 4096 bits is legal
        }

        public static bool IsLegalKeySize(int bits, KeySizes[] legalSizes)
        {
            foreach (var range in legalSizes)
            {
                if (bits < range.MinSize || bits > range.MaxSize)
                    continue;

                if (range.SkipSize == 0)
                    return bits == range.MinSize;

                if ((bits - range.MinSize) % range.SkipSize == 0)
                    return true;
            }

            return false;
        }
    }
}