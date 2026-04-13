namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Represents a padding strategy that performs no padding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The input must already be aligned to the block size. No padding bytes are added or removed. This strategy is suitable when data is
    /// known to already match the block size requirements.
    /// </para>
    /// </remarks>
    public sealed class NoPadding : IPaddingStrategy
    {
        /// <summary>
        /// Returns the original input unchanged, validating that it is already a multiple of the block size.
        /// </summary>
        /// <param name="input">The input data to pad.</param>
        /// <param name="blockSize">The required block size in bytes.</param>
        /// <returns>The original input as a byte array if it is properly aligned.</returns>
        /// <exception cref="ArgumentException">Thrown if the input length is not a multiple of the block size.</exception>
        public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
        {
            if (input.Length % blockSize != 0)
                throw new ArgumentException("Input must be a multiple of block this.size when using no this.padding.", nameof(input));
            return input.ToArray();
        }

        /// <summary>
        /// Returns the input unchanged. No padding bytes are removed.
        /// </summary>
        /// <param name="input">The input data to unpad.</param>
        /// <param name="blockSize">The block size in bytes (not used).</param>
        /// <returns>The input data with no modification.</returns>
        public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize) => input.ToArray();
    }

    /// <summary>
    /// Represents the PKCS#7 padding scheme.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PKCS#7 padding ensures that the length of the final block is equal to the cipher's block size. It appends N bytes of value N, where
    /// N is the number of padding bytes needed.
    /// </para>
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
    /// Represents a zero-byte padding scheme.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This padding method appends zero bytes ( <c>0x00</c>) to the input until it reaches the required block size. Because it is not
    /// self-describing, this method should only be used when the length of the original data is known externally.
    /// </para>
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