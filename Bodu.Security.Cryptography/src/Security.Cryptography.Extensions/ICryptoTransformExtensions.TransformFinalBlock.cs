// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensions.TransformFinalBlock.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class ICryptoTransformExtensions
{
    /// <summary>
    /// Finalizes the transformation without processing any additional input data.
    /// </summary>
    /// <param name="cryptoTransform">
    /// The cryptographic transform to finalize. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// A new byte array containing the final block output, which may be empty or contain padding bytes depending on the
    /// transform.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cryptoTransform" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload when the transform should be finalized without processing any remaining data.
    /// </para>
    /// </remarks>
    public static byte[] TransformFinalBlock(this ICryptoTransform cryptoTransform)
    {
        ThrowHelper.ThrowIfNull(cryptoTransform);

        return cryptoTransform.TransformFinalBlock([], 0, 0);
    }

    /// <summary>
    /// Finalizes the transformation of the entire specified byte array.
    /// </summary>
    /// <param name="cryptoTransform">
    /// The cryptographic transform to finalize. Must not be <see langword="null" />.
    /// </param>
    /// <param name="array">The input array to transform. Must not be <see langword="null" />.</param>
    /// <returns>A new byte array containing the final transformed output.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cryptoTransform" /> or <paramref name="array" /> is <see langword="null" />.
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
    /// Finalizes the transformation of the specified byte array beginning at the given offset.
    /// </summary>
    /// <param name="cryptoTransform">
    /// The cryptographic transform to finalize. Must not be <see langword="null" />.
    /// </param>
    /// <param name="array">The input array to transform. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="array" /> at which to begin reading.</param>
    /// <returns>A new byte array containing the final transformed output.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cryptoTransform" /> or <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> is negative or exceeds the length of <paramref name="array" />.
    /// </exception>
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
    /// <param name="cryptoTransform">
    /// The cryptographic transform to finalize. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">The input span of bytes to transform.</param>
    /// <returns>A new byte array containing the final transformed output.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cryptoTransform" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload writes the input through a <see cref="CryptoStream" /> and finalizes the block. It avoids array
    /// copies for the input data while still producing a new byte array as output.
    /// </para>
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

        using var ms = new MemoryStream(input.Length + cryptoTransform.OutputBlockSize);

        // leaveOpen: true so that ms remains valid for ToArray() after cryptoStream is disposed.
        using var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write, leaveOpen: true);

        cryptoStream.Write(input);
        cryptoStream.FlushFinalBlock();

        return ms.ToArray();
    }

    /// <summary>
    /// Finalizes the transformation of the specified memory region.
    /// </summary>
    /// <param name="cryptoTransform">
    /// The cryptographic transform to finalize. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">The input memory region to transform.</param>
    /// <returns>A new byte array containing the final transformed output.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cryptoTransform" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload delegates to <see cref="TransformFinalBlock(ICryptoTransform, ReadOnlySpan{byte})" />.
    /// </para>
    /// </remarks>
    public static byte[] TransformFinalBlock(this ICryptoTransform cryptoTransform, ReadOnlyMemory<byte> input)
        => cryptoTransform.TransformFinalBlock(input.Span);
}
