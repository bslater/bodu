using System;
using System.IO;
using System.Security.Cryptography;
using Bodu;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class ICryptoTransformExtensions
    {
        /// <summary>
        /// Finalizes the transformation without processing any input data.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to finalize.</param>
        /// <returns>The final block output produced by the transform, which may be empty or contain padding.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="cryptoTransform" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>Use this overload when the transform should be finalized without processing any additional data.</para>
        /// </remarks>
        public static byte[] TransformFinalBlock(this ICryptoTransform cryptoTransform)
        {
            ThrowHelper.ThrowIfNull(cryptoTransform);
            return cryptoTransform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        }

        /// <summary>
        /// Finalizes the transformation of the specified byte array.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to finalize.</param>
        /// <param name="array">The input array to transform.</param>
        /// <returns>A new byte array containing the final transformed output.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="cryptoTransform"/> or <paramref name="array"/> is <see langword="null" />.
        /// </exception>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// byte[] result = aes.CreateEncryptor().TransformFinalBlock(data);
        ///]]>
        /// </code>
        /// </example>
        public static byte[] TransformFinalBlock(this ICryptoTransform cryptoTransform, byte[] array)
        {
            ThrowHelper.ThrowIfNull(cryptoTransform);
            ThrowHelper.ThrowIfNull(array);

            return cryptoTransform.TransformFinalBlock(array, 0, array.Length);
        }

        /// <summary>
        /// Finalizes the transformation starting at the specified offset to the end of the array.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to finalize.</param>
        /// <param name="array">The input array to transform.</param>
        /// <param name="offset">The zero-based byte offset in the array to begin transforming.</param>
        /// <returns>A new byte array containing the final transformed output.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="cryptoTransform" /> or <paramref name="array" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="offset" /> is negative or exceeds the array bounds.</exception>
        public static byte[] TransformFinalBlock(this ICryptoTransform cryptoTransform, byte[] array, int offset)
        {
            ThrowHelper.ThrowIfNull(cryptoTransform);
            ThrowHelper.ThrowIfNull(array);
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, array.Length - offset);

            return cryptoTransform.TransformFinalBlock(array, offset, array.Length - offset);
        }

        /// <summary>
        /// Finalizes the transformation of the specified input span.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to finalize.</param>
        /// <param name="input">The input span of bytes to transform.</param>
        /// <returns>A new byte array containing the final transformed output.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="cryptoTransform"/> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method uses a <see cref="CryptoStreamBen"/> to write the input and finalize the block.</para>
        /// <para>It avoids intermediate allocations and is suitable for performance-sensitive scenarios.</para>
        /// </remarks>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// byte[] result = aes.CreateEncryptor().TransformFinalBlock(data.AsSpan());
        ///]]>
        /// </code>
        /// </example>
        public static byte[] TransformFinalBlock(this ICryptoTransform cryptoTransform, ReadOnlySpan<byte> input)
        {
            ThrowHelper.ThrowIfNull(cryptoTransform);

            using MemoryStream ms = new MemoryStream(input.Length + cryptoTransform.OutputBlockSize);
            using CryptoStreamBen cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write);

            cryptoStream.Write(input);
            cryptoStream.FlushFinalBlock();

            return ms.ToArray();
        }

        /// <summary>
        /// Finalizes the transformation of the specified memory region.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to finalize.</param>
        /// <param name="input">The input memory region to transform.</param>
        /// <returns>A new byte array containing the final transformed output.</returns>
        /// <remarks>
        /// <para>This is a convenience wrapper over <see cref="TransformFinalBlock(ICryptoTransform, ReadOnlySpan{byte})" />.</para>
        /// </remarks>
        public static byte[] TransformFinalBlock(this ICryptoTransform cryptoTransform, ReadOnlyMemory<byte> input)
            => cryptoTransform.TransformFinalBlock(input.Span);
    }
}