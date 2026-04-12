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
        private const string SimpleText = "The quick brown fox jumps over the lazy dog";
        private static readonly byte[] emptyByteArray = Array.Empty<byte>();
        private static readonly byte[] simpleTextAsciiBytes = Encoding.ASCII.GetBytes(SimpleText);
        private static readonly byte[] byteSequence0To255 = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        /// <summary>
        /// Gets a shared, empty byte array instance.
        /// </summary>
        public static byte[] EmptyByteArray => emptyByteArray;

        /// <summary>
        /// Gets the ASCII-encoded byte representation of the <see cref="SimpleText" />.
        /// </summary>
        public static byte[] SimpleTextAsciiBytes => simpleTextAsciiBytes;

        /// <summary>
        /// Gets a byte array containing values from 0 to 255 in ascending order.
        /// </summary>
        public static byte[] ByteSequence0To255 => byteSequence0To255;

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
    }
}