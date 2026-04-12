using System;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class ICryptoTransformExtensions
    {
        /// <summary>
        /// Applies the transform to the entire input buffer and writes the result back to the same array.
        /// </summary>
        /// <param name="cryptoTransform">The cryptographic transform to apply.</param>
        /// <param name="array">The input byte array to transform. The result is written in-place.</param>
        /// <returns>The number of bytes written to the <paramref name="array"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="cryptoTransform"/> or <paramref name="array"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the input buffer length is not a multiple of the transform's <see cref="ICryptoTransform.InputBlockSize"/>.
        /// </exception>
        /// <remarks>
        /// <para>This method performs an in-place block transformation using the underlying <c>TransformBlock</c> method.</para>
        /// <para>
        /// The input must be a multiple of <see cref="ICryptoTransform.InputBlockSize"/> and large enough to accommodate
        /// the transform's output requirements.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// byte[] block = GetNextBlock(); // must be block-size aligned
        /// aes.CreateEncryptor().TransformBlock(block);
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
}