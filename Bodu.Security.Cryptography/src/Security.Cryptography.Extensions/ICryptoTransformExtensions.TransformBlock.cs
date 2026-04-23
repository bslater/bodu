// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensions.TransformBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class ICryptoTransformExtensions
{
    /// <summary>
    /// Applies the transform to the entire input buffer, writing the result back into the same array.
    /// </summary>
    /// <param name="cryptoTransform">The cryptographic transform to apply. Must not be <see langword="null" />.</param>
    /// <param name="array">The input byte array to transform in-place. Must not be <see langword="null" />.</param>
    /// <returns>The number of bytes written into <paramref name="array" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cryptoTransform" /> or <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the length of <paramref name="array" /> is not a multiple of the transform's
    /// <see cref="ICryptoTransform.InputBlockSize" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload is a convenience wrapper over <see cref="ICryptoTransform.TransformBlock" /> that reads and writes
    /// to the same buffer. The input length must satisfy the block-alignment requirements of the underlying transform.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// using var aes = Aes.Create();
    /// byte[] block = GetNextBlock(); // must be block-size aligned
    /// int written = aes.CreateEncryptor().TransformBlock(block);
    ///]]>
    /// </code>
    /// </example>
    public static int TransformBlock(this ICryptoTransform cryptoTransform, byte[] array)
    {
        ThrowHelper.ThrowIfNull(cryptoTransform);
        ThrowHelper.ThrowIfNull(array);

        return cryptoTransform.TransformBlock(array, 0, array.Length, array, 0);
    }
}
