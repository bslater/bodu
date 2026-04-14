namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Defines a symmetric block cipher that encrypts and decrypts data one fixed-size block at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations represent primitives such as AES or Threefish and operate on buffers whose length equals <see cref="BlockSize" />.
    /// This interface intentionally exposes only the raw block primitive; chaining modes, padding, and IV management are the responsibility
    /// of higher-level components such as <see cref="IBlockCipherModeTransform" /> and <see cref="IPaddingStrategy" />.
    /// </para>
    /// <para>
    /// Implementations must release all sensitive key material when <see cref="IDisposable.Dispose" /> is called and should be safe to
    /// invoke repeatedly for the lifetime of the instance.
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
        /// Decrypts a single block of ciphertext into the specified output span.
        /// </summary>
        /// <param name="input">A read-only span containing the ciphertext block. Its length must equal <see cref="BlockSize" />.</param>
        /// <param name="output">A writable span that receives the plaintext block. Its length must equal <see cref="BlockSize" />.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the length of <paramref name="input" /> or <paramref name="output" /> does not match <see cref="BlockSize" />.
        /// </exception>
        /// <remarks>
        /// In-place decryption (passing the same buffer as both <paramref name="input" /> and <paramref name="output" />) is supported only
        /// when the implementation explicitly permits it; otherwise the spans must not overlap.
        /// </remarks>
        void Decrypt(ReadOnlySpan<byte> input, Span<byte> output);

        /// <summary>
        /// Encrypts a single block of plaintext into the specified output span.
        /// </summary>
        /// <param name="input">A read-only span containing the plaintext block. Its length must equal <see cref="BlockSize" />.</param>
        /// <param name="output">A writable span that receives the ciphertext block. Its length must equal <see cref="BlockSize" />.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the length of <paramref name="input" /> or <paramref name="output" /> does not match <see cref="BlockSize" />.
        /// </exception>
        /// <remarks>
        /// In-place encryption (passing the same buffer as both <paramref name="input" /> and <paramref name="output" />) is supported only
        /// when the implementation explicitly permits it; otherwise the spans must not overlap.
        /// </remarks>
        void Encrypt(ReadOnlySpan<byte> input, Span<byte> output);
    }
}