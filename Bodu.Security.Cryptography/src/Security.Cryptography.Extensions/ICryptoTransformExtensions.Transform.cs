// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensions.Transform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class ICryptoTransformExtensions
{
    /// <summary>
    /// Transforms the entire input byte array using the specified cryptographic transform.
    /// </summary>
    /// <param name="cryptoTransform">The cryptographic transform to apply. Must not be <see langword="null" />.</param>
    /// <param name="array">The input byte array to transform. Must not be <see langword="null" />.</param>
    /// <returns>A new byte array containing the transformed output.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cryptoTransform" /> or <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload is equivalent to calling <c>Transform(array, 0, array.Length)</c>.
    /// </para>
    /// </remarks>
    public static byte[] Transform(this ICryptoTransform cryptoTransform, byte[] array)
    {
        ThrowHelper.ThrowIfNull(cryptoTransform);
        ThrowHelper.ThrowIfNull(array);

        return cryptoTransform.Transform(array, 0, array.Length);
    }

    /// <summary>
    /// Transforms a portion of the specified byte array using the given cryptographic transform.
    /// </summary>
    /// <param name="cryptoTransform">The cryptographic transform to apply. Must not be <see langword="null" />.</param>
    /// <param name="array">
    /// The input byte array containing the data to transform. Must not be <see langword="null" />.
    /// </param>
    /// <param name="offset">The zero-based index in <paramref name="array" /> at which to begin reading.</param>
    /// <param name="count">The number of bytes to transform.</param>
    /// <returns>A new byte array containing the transformed output.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cryptoTransform" /> or <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is negative, or exceeds the bounds of
    /// <paramref name="array" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the sum of <paramref name="offset" /> and <paramref name="count" /> exceeds the length of
    /// <paramref name="array" />.
    /// </exception>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// using var aes = Aes.Create();
    /// byte[] encrypted = aes.CreateEncryptor().Transform(data, 0, data.Length);
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
    /// <param name="cryptoTransform">The cryptographic transform to apply. Must not be <see langword="null" />.</param>
    /// <param name="input">The span of input bytes to transform.</param>
    /// <returns>A new byte array containing the transformed output.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cryptoTransform" /> is <see langword="null" />.
    /// </exception>
    public static byte[] Transform(this ICryptoTransform cryptoTransform, ReadOnlySpan<byte> input)
    {
        ThrowHelper.ThrowIfNull(cryptoTransform);

        return TransformInternal(cryptoTransform, input);
    }

    /// <summary>
    /// Transforms a memory region using the specified cryptographic transform.
    /// </summary>
    /// <param name="cryptoTransform">The cryptographic transform to apply. Must not be <see langword="null" />.</param>
    /// <param name="input">The memory region of input bytes to transform.</param>
    /// <returns>A new byte array containing the transformed output.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cryptoTransform" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload delegates to <see cref="Transform(ICryptoTransform, ReadOnlySpan{byte})" />.
    /// </para>
    /// </remarks>
    public static byte[] Transform(this ICryptoTransform cryptoTransform, ReadOnlyMemory<byte> input)
        => cryptoTransform.Transform(input.Span);

    /// <summary>
    /// Applies the cryptographic transform to data read from <paramref name="sourceStream" /> and writes the result to
    /// <paramref name="targetStream" /> using the specified buffer size.
    /// </summary>
    /// <param name="transform">The cryptographic transform to apply. Must not be <see langword="null" />.</param>
    /// <param name="sourceStream">
    /// The stream to read untransformed data from. Must not be <see langword="null" />.
    /// </param>
    /// <param name="targetStream">The stream to write transformed data to. Must not be <see langword="null" />.</param>
    /// <param name="bufferSize">The size, in bytes, of the temporary read buffer. Must be greater than zero.</param>
    /// <returns>The total number of bytes read from <paramref name="sourceStream" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="transform" />, <paramref name="sourceStream" />, or <paramref name="targetStream" />
    /// is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bufferSize" /> is less than or equal to zero.
    /// </exception>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// using var aes = Aes.Create();
    /// using var decryptor = aes.CreateDecryptor();
    /// long bytesRead = decryptor.Transform(encryptedStream, outputStream, 81920);
    ///]]>
    /// </code>
    /// </example>
    public static int Transform(this ICryptoTransform transform, Stream sourceStream, Stream targetStream, int bufferSize)
    {
        ThrowHelper.ThrowIfNull(transform);
        ThrowHelper.ThrowIfNull(sourceStream);
        ThrowHelper.ThrowIfNull(targetStream);
        ThrowHelper.ThrowIfLessThanOrEqual(bufferSize, 0);

        // Use a pooled buffer cleared on return so plaintext read from sourceStream cannot leak
        // to a subsequent pool consumer.
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            // leaveOpen: true because targetStream is caller-owned and must not be disposed here.
            var cryptoStream = new CryptoStream(targetStream, transform, CryptoStreamMode.Write, leaveOpen: true);
            bool completed = false;
            try
            {
                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = sourceStream.Read(buffer, 0, bufferSize)) > 0)
                {
                    cryptoStream.Write(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                }

                cryptoStream.FlushFinalBlock();
                completed = true;
                return totalBytesRead;
            }
            finally
            {
                if (completed)
                {
                    cryptoStream.Dispose();
                }
                else
                {
                    // The primary flow failed before FlushFinalBlock completed, so Dispose will
                    // attempt to finalize an incomplete transform and raise a CryptographicException
                    // of its own. Swallow that secondary exception so the original cause propagates.
                    try
                    {
                        cryptoStream.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    /// <summary>
    /// Transforms the input span and writes the result into the specified destination span.
    /// </summary>
    /// <param name="transform">The cryptographic transform to apply. Must not be <see langword="null" />.</param>
    /// <param name="input">The input span to transform.</param>
    /// <param name="destination">The destination span to receive the transformed output.</param>
    /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="transform" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to hold the transformed output. A safe minimum size is
    /// <c>input.Length + transform.OutputBlockSize</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The transformed output is written into <paramref name="destination" /> rather than returned as a new allocation,
    /// making this overload suitable for scenarios where the caller manages buffer lifetimes.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the transform has been disposed or is in an invalid state.
    /// </exception>
    public static int Transform(this ICryptoTransform transform, ReadOnlySpan<byte> input, Span<byte> destination)
    {
        ThrowHelper.ThrowIfNull(transform);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, 0, input.Length + transform.OutputBlockSize);

        using var ms = new MemoryStream(input.Length + transform.OutputBlockSize);
        using var cryptoStream = new CryptoStream(ms, transform, CryptoStreamMode.Write, leaveOpen: true);

        cryptoStream.Write(input);
        cryptoStream.FlushFinalBlock();

        ArraySegment<byte> segment = CryptographyHelper.GetBufferOrThrowIfInaccessible(ms);
        segment.AsSpan().CopyTo(destination);
        return segment.Count;
    }

    /// <summary>
    /// Transforms the input memory region and writes the result into the destination memory region.
    /// </summary>
    /// <param name="transform">The cryptographic transform to apply. Must not be <see langword="null" />.</param>
    /// <param name="input">The memory region containing the input data.</param>
    /// <param name="destination">The memory region to receive the transformed output.</param>
    /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="transform" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to hold the transformed output.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload delegates to <see cref="Transform(ICryptoTransform, ReadOnlySpan{byte}, Span{byte})" />.
    /// </para>
    /// </remarks>
    public static int Transform(this ICryptoTransform transform, ReadOnlyMemory<byte> input, Memory<byte> destination)
        => transform.Transform(input.Span, destination.Span);

    /// <summary>
    /// Performs the core transformation logic on a span of bytes using a <see cref="CryptoStream" />.
    /// </summary>
    /// <param name="cryptoTransform">The cryptographic transform to apply.</param>
    /// <param name="input">The span of input bytes to transform.</param>
    /// <returns>The transformed output as a new byte array.</returns>
    /// <remarks>
    /// <para>
    /// Used internally by the public <c>Transform</c> overloads. Callers are responsible for all argument validation
    /// before invoking this method.
    /// </para>
    /// </remarks>
    internal static byte[] TransformInternal(ICryptoTransform cryptoTransform, ReadOnlySpan<byte> input)
    {
        using var ms = new MemoryStream(input.Length + cryptoTransform.OutputBlockSize);

        // leaveOpen: true so that ms remains valid for ToArray() after cryptoStream is disposed.
        using var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write, leaveOpen: true);

        cryptoStream.Write(input);
        cryptoStream.FlushFinalBlock();

        return ms.ToArray();
    }
}
