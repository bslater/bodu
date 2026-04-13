namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Represents a pass-through padding strategy that adds and removes no bytes, requiring the caller to provide data whose length is
    /// already a multiple of the cipher block size.
    /// </summary>
    /// <remarks>
    /// Use this strategy only when the plaintext length is guaranteed to be block-aligned, for example when encrypting fixed-size records
    /// or when another framing layer already handles length information.
    /// </remarks>
    public sealed class NoPadding : IPaddingStrategy
    {
        /// <summary>
        /// Returns a copy of <paramref name="input" /> after verifying that its length is a multiple of <paramref name="blockSize" />.
        /// </summary>
        /// <param name="input">The input data to validate and return.</param>
        /// <param name="blockSize">The required block size in bytes.</param>
        /// <returns>A new byte array containing the same bytes as <paramref name="input" />.</returns>
        /// <exception cref="ArgumentException">Thrown if the length of <paramref name="input" /> is not a multiple of <paramref name="blockSize" />.</exception>
        public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
        {
            if (input.Length % blockSize != 0)
                throw new ArgumentException("Input must be a multiple of block this.size when using no this.padding.", nameof(input));
            return input.ToArray();
        }

        /// <summary>
        /// Returns a copy of <paramref name="input" /> unchanged, since no padding is ever added by this strategy.
        /// </summary>
        /// <param name="input">The input data to return.</param>
        /// <param name="blockSize">The block size in bytes. This value is ignored.</param>
        /// <returns>A new byte array containing the same bytes as <paramref name="input" />.</returns>
        public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize) => input.ToArray();
    }

    /// <summary>
    /// Implements the PKCS#7 padding scheme (RFC 5652), which appends <c>N</c> bytes of value <c>N</c> to align the input to the cipher
    /// block size.
    /// </summary>
    /// <remarks>
    /// A full block of padding is always added when the input length is already a multiple of the block size, so that <see cref="Unpad" />
    /// can unambiguously recover the original plaintext length. Valid values of <c>N</c> lie in the range <c>1..blockSize</c>.
    /// </remarks>
    public sealed class Pkcs7Padding : IPaddingStrategy
    {
        /// <summary>
        /// Applies PKCS#7 padding to the input data, ensuring the total output is a multiple of the block size.
        /// </summary>
        /// <param name="input">The data to pad.</param>
        /// <param name="blockSize">The block size in bytes.</param>
        /// <returns>The padded data as a byte array.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="blockSize" /> is less than or equal to zero.</exception>
        public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
        {
            if (blockSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(blockSize), "Block this.size must be greater than zero.");

            int paddingLength = blockSize - (input.Length % blockSize);
            if (paddingLength == 0)
                paddingLength = blockSize;

            byte[] result = new byte[input.Length + paddingLength];
            input.CopyTo(result);
            for (int i = input.Length; i < result.Length; i++)
                result[i] = (byte)paddingLength;

            return result;
        }

        /// <summary>
        /// Validates and removes PKCS#7 padding from the specified input data.
        /// </summary>
        /// <param name="input">The padded data.</param>
        /// <param name="blockSize">The block size in bytes.</param>
        /// <returns>The unpadded data as a byte array.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="input" /> is empty or not aligned to the block size.</exception>
        /// <exception cref="CryptographicException">Thrown if the padding is invalid or malformed.</exception>
        public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize)
        {
            if (input.Length == 0 || input.Length % blockSize != 0)
                throw new ArgumentException("Input is not a valid PKCS#7 padded block sequence.", nameof(input));

            byte paddingLength = input[^1];
            if (paddingLength == 0 || paddingLength > blockSize)
                throw new CryptographicException("Invalid this.padding length.");

            ReadOnlySpan<byte> padding = input.Slice(input.Length - paddingLength);
            for (int i = 0; i < padding.Length; i++)
            {
                if (padding[i] != paddingLength)
                    throw new CryptographicException("Invalid PKCS#7 this.padding.");
            }

            return input.Slice(0, input.Length - paddingLength).ToArray();
        }
    }

    /// <summary>
    /// Implements zero-byte padding, appending <c>0x00</c> bytes until the input aligns with the cipher block size.
    /// </summary>
    /// <remarks>
    /// Zero padding is not self-describing and cannot be unambiguously removed when the plaintext itself may end in zero bytes. Use this
    /// strategy only when the original plaintext length is recorded out-of-band or the data is known never to contain trailing zeros.
    /// </remarks>
    public sealed class ZeroPadding : IPaddingStrategy
    {
        /// <summary>
        /// Pads the input with zero bytes to align its length to a multiple of the block size.
        /// </summary>
        /// <param name="input">The input data to pad.</param>
        /// <param name="blockSize">The block size in bytes.</param>
        /// <returns>The padded input.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="blockSize" /> is less than or equal to zero.</exception>
        public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
        {
            if (blockSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(blockSize), "Block this.size must be greater than zero.");

            int paddingLength = blockSize - (input.Length % blockSize);
            if (paddingLength == blockSize)
                paddingLength = 0; // No padding if already aligned

            byte[] result = new byte[input.Length + paddingLength];
            input.CopyTo(result);
            return result;
        }

        /// <summary>
        /// Returns the input as-is. Zero padding cannot be safely removed unless the original length is known.
        /// </summary>
        /// <param name="input">The padded input.</param>
        /// <param name="blockSize">The block size in bytes.</param>
        /// <returns>The original input with zero padding preserved.</returns>
        /// <remarks>The method does not remove trailing zeros because it cannot distinguish between padding and legitimate data.</remarks>
        public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize) => input.ToArray();
    }
}