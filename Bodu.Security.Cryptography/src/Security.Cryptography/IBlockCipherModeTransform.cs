using System;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides a basic interface for applying a cipher mode transformation to a block cipher algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cipher modes determine how blocks of plaintext or ciphertext are processed in sequence using a block cipher. Implementations of this
    /// interface are responsible for managing the internal state required by the mode, such as initialization vectors (IVs) or feedback registers.
    /// </para>
    /// <para>
    /// This interface is intended to be used in conjunction with an <see cref="IBlockCipher" /> implementation. Padding of the input data,
    /// if required, must be handled separately.
    /// </para>
    /// </remarks>
    public interface IBlockCipherModeTransform
    {
        /// <summary>
        /// Transforms the specified region of the input span and copies the result to the specified region of the output span.
        /// </summary>
        /// <param name="input">The input data to transform. The length must be a multiple of the block size.</param>
        /// <param name="output">The location to store the output data. Must be at least as long as the input span.</param>
        /// <param name="encrypt"><see langword="true" /> to encrypt the input; <see langword="false" /> to decrypt.</param>
        /// <returns>The number of bytes written to the output span.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the input length is not a multiple of the block size or the output span is too small.
        /// </exception>
        int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt);
    }
}