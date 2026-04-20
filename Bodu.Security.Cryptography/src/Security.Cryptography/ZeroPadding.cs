namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Implements zero-byte padding, appending <c>0x00</c> bytes until the input aligns with the cipher block size.
    /// </summary>
    /// <remarks>
    /// Zero padding is not self-describing and cannot be unambiguously removed when the plaintext itself may end in zero bytes. Use this
    /// strategy only when the original plaintext length is recorded out-of-band or the data is known never to contain trailing zeros.
    /// </remarks>
    public sealed class ZeroPadding : IPaddingStrategy
    {
        /// <inheritdoc />
        public bool StripsPaddingOnUnpad => false;

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
                throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be greater than zero.");

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
