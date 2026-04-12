using System;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides the interface for a symmetric block cipher algorithm that processes data in fixed-size blocks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations of this interface represent symmetric encryption algorithms such as AES, Threefish, or others that operate on blocks
    /// of data of a specific size. The interface supports both encryption and decryption of blocks.
    /// </para>
    /// <para>
    /// Implementations must ensure correct handling of fixed-size input and output spans and maintain consistent behavior across multiple
    /// calls. Implementations must also release all sensitive resources when <see cref="IDisposable.Dispose" /> is called.
    /// </para>
    /// </remarks>
    public interface IBlockCipher
        : System.IDisposable
    {
        /// <summary>
        /// Gets the fixed block size (in bytes) that the cipher operates on.
        /// </summary>
        /// <value>The block size in bytes, such as 16 for 128-bit block ciphers.</value>
        int BlockSize { get; }

        /// <summary>
        /// Performs the decryption of a single block of input data into the specified output span.
        /// </summary>
        /// <param name="input">A read-only span containing the encrypted input block. The length must match <see cref="BlockSize" />.</param>
        /// <param name="output">A writable span where the decrypted block will be written. The length must match <see cref="BlockSize" />.</param>
        /// <exception cref="ArgumentException">Thrown if the input or output span does not match the expected block size.</exception>
        /// <remarks>
        /// The method performs in-place decryption if the same buffer is used for both <paramref name="input" /> and
        /// <paramref name="output" />, assuming the implementation permits it.
        /// </remarks>
        void Decrypt(ReadOnlySpan<byte> input, Span<byte> output);

        /// <summary>
        /// Performs the encryption of a single block of input data into the specified output span.
        /// </summary>
        /// <param name="input">A read-only span containing the plaintext input block. The length must match <see cref="BlockSize" />.</param>
        /// <param name="output">A writable span where the encrypted block will be written. The length must match <see cref="BlockSize" />.</param>
        /// <exception cref="ArgumentException">Thrown if the input or output span does not match the expected block size.</exception>
        /// <remarks>
        /// The method performs in-place encryption if the same buffer is used for both <paramref name="input" /> and
        /// <paramref name="output" />, assuming the implementation permits it.
        /// </remarks>
        void Encrypt(ReadOnlySpan<byte> input, Span<byte> output);
    }
}