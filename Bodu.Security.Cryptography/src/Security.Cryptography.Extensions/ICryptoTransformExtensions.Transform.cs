using System;
using System.IO;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class ICryptoTransformExtensions
    {
        /// <summary>
        /// Transforms the entire byte array using the specified cryptographic transform.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to apply.</param>
        /// <param name="array">The input byte array to transform.</param>
        /// <returns>A new byte array containing the transformed output.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="cryptoTransform" /> or <paramref name="array" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the range is invalid.</exception>
        /// <remarks>
        /// <para>This method is equivalent to calling <c>Transform(array, 0, array.Length)</c>.</para>
        /// </remarks>
        public static byte[] Transform(this ICryptoTransform cryptoTransform, byte[] array)
            => cryptoTransform.Transform(array, 0, array?.Length ?? 0);

        /// <summary>
        /// Transforms a portion of the specified byte array using the given cryptographic transform.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to apply.</param>
        /// <param name="array">The input byte array containing the data to transform.</param>
        /// <param name="offset">The zero-based index in <paramref name="array"/> at which to begin reading.</param>
        /// <param name="count">The number of bytes to transform.</param>
        /// <returns>A new byte array containing the transformed output.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="cryptoTransform"/> or <paramref name="array"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="offset"/> or <paramref name="count"/> is out of range for the array.</exception>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// byte[] encrypted = aes.Encrypt(data);
        /// byte[] decrypted = aes.Decrypt(encrypted, 0, encrypted.Length);
        ///]]>
        /// </code>
        /// </example>
        public static byte[] Transform(this ICryptoTransform cryptoTransform, byte[] array, int offset, int count)
        {
            ThrowHelper.ThrowIfNull(cryptoTransform);
            ThrowHelper.ThrowIfNull(array);
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);

            return TransformInternal(cryptoTransform, array.AsSpan(offset, count));
        }

        /// <summary>
        /// Transforms a span of bytes using the specified cryptographic transform.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to apply.</param>
        /// <param name="input">The span of input bytes to transform.</param>
        /// <returns>A new byte array containing the transformed output.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="cryptoTransform" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This overload avoids intermediate array allocations and is suitable for high-performance usage.</para>
        /// </remarks>
        public static byte[] Transform(this ICryptoTransform cryptoTransform, ReadOnlySpan<byte> input)
        {
            ThrowHelper.ThrowIfNull(cryptoTransform);
            return TransformInternal(cryptoTransform, input);
        }

        /// <summary>
        /// Transforms a memory region using the specified cryptographic transform.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to apply.</param>
        /// <param name="input">The memory region of input bytes to transform.</param>
        /// <returns>A new byte array containing the transformed output.</returns>
        /// <remarks>
        /// <para>This is a convenience wrapper over the <see cref="Transform(ICryptoTransform, ReadOnlySpan{byte})" /> overload.</para>
        /// </remarks>
        public static byte[] Transform(this ICryptoTransform cryptoTransform, ReadOnlyMemory<byte> input)
            => cryptoTransform.Transform(input.Span);

        /// <summary>
        /// Applies the cryptographic transform to data read from <paramref name="sourceStream"/>
        /// and writes the result to <paramref name="targetStream"/> using the specified buffer size.
        /// </summary>
        /// <param name="transform">The cryptographic transform to apply.</param>
        /// <param name="sourceStream">The stream to read untransformed data from.</param>
        /// <param name="targetStream">The stream to write transformed data to.</param>
        /// <param name="bufferSize">The size, in bytes, of the temporary read buffer. Must be greater than zero.</param>
        /// <returns>The total number of bytes read from <paramref name="sourceStream"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="transform"/>, <paramref name="sourceStream"/>, or <paramref name="targetStream"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bufferSize"/> is less than or equal to zero.</exception>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// using var decryptor = aes.CreateDecryptor();
        /// decryptor.Transform(encryptedStream, outputStream, 81920);
        ///]]>
        /// </code>
        /// </example>
        public static int Transform(this ICryptoTransform transform, Stream sourceStream, Stream targetStream, int bufferSize)
        {
            ThrowHelper.ThrowIfNull(transform);
            ThrowHelper.ThrowIfNull(sourceStream);
            ThrowHelper.ThrowIfNull(targetStream);
            ThrowHelper.ThrowIfLessThanOrEqual(bufferSize, 0);

            byte[] buffer = new byte[bufferSize];
            int totalBytesRead = 0;

            using CryptoStreamBen cryptoStream = new CryptoStreamBen(targetStream, transform, CryptoStreamMode.Write, leaveOpen: true);
            int bytesRead;

            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                cryptoStream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;
            }

            cryptoStream.FlushFinalBlock();
            return totalBytesRead;
        }

        /// <summary>
        /// Transforms the input span and writes the result into the specified destination span.
        /// </summary>
        /// <param name="transform">The cryptographic transform to apply.</param>
        /// <param name="input">The input span to transform.</param>
        /// <param name="destination">The destination span to write the transformed output.</param>
        /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="transform" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="destination" /> is too small.</exception>
        /// <remarks>This method avoids allocations by writing directly to the caller-provided buffer.</remarks>
        public static int Transform(this ICryptoTransform transform, ReadOnlySpan<byte> input, Span<byte> destination)
        {
            ThrowHelper.ThrowIfNull(transform);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(destination, 0, input.Length + transform.OutputBlockSize);

            using var ms = new MemoryStream(destination.Length);
            using var cryptoStream = new CryptoStreamBen(ms, transform, CryptoStreamMode.Write);
            cryptoStream.Write(input);
            cryptoStream.FlushFinalBlock();

            if (!ms.TryGetBuffer(out ArraySegment<byte> segment))
                throw new InvalidOperationException("Failed to access transformed data.");

            segment.AsSpan().CopyTo(destination);
            return segment.Count;
        }

        /// <summary>
        /// Transforms the input memory region and writes the result into the destination memory region.
        /// </summary>
        /// <param name="transform">The cryptographic transform to apply.</param>
        /// <param name="input">The memory region containing the input data.</param>
        /// <param name="destination">The memory region to write the transformed result.</param>
        /// <returns>The number of bytes written.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="transform" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="destination" /> is too small.</exception>
        public static int Transform(this ICryptoTransform transform, ReadOnlyMemory<byte> input, Memory<byte> destination)
            => transform.Transform(input.Span, destination.Span);

        /// <summary>
        /// Performs the core transformation logic on a span of bytes using a <see cref="CryptoStreamBen" />.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to apply.</param>
        /// <param name="input">The span of input bytes to transform.</param>
        /// <returns>The transformed output as a byte array.</returns>
        /// <remarks>
        /// <para>This method is used internally by the public <c>Transform</c> overloads and avoids redundant validation.</para>
        /// </remarks>
        internal static byte[] TransformInternal(ICryptoTransform cryptoTransform, ReadOnlySpan<byte> input)
        {
            using MemoryStream ms = new MemoryStream(input.Length + cryptoTransform.OutputBlockSize);
            using CryptoStreamBen cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write);

            cryptoStream.Write(input);
            cryptoStream.FlushFinalBlock();

            return ms.ToArray();
        }
    }
}