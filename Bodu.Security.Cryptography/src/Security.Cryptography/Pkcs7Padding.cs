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
                throw new ArgumentException("Input must be a multiple of block size when using no padding.", nameof(input));
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
                throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be greater than zero.");

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
            // Constant-time padding verification to mitigate CBC padding-oracle attacks (Vaudenay 2002).
            if (input.Length == 0 || input.Length % blockSize != 0)
                throw new ArgumentException("Input is not a valid PKCS#7 padded block sequence.", nameof(input));

            int length = input.Length;
            int padLen = input[length - 1];

            // valid = (padLen >= 1) & (padLen <= blockSize), as a 0/1 mask, branchlessly.
            // padLen is a byte value in [0, 255]; (-padLen) is in [-255, 0] and its sign bit
            // is set iff padLen > 0, which gives geOne = 1 iff padLen >= 1.
            int geOne = ((-padLen) >> 31) & 1;                  // 1 iff padLen >= 1
            int leBlock = ((padLen - blockSize - 1) >> 31) & 1; // 1 iff padLen <= blockSize
            int valid = geOne & leBlock;

            // effective = valid == 1 ? padLen : blockSize  (branchless)
            int effective = (valid * padLen) + ((1 - valid) * blockSize);

            // Walk the last blockSize bytes unconditionally.
            int start = length - blockSize;
            for (int i = start; i < length; i++)
            {
                // shouldBePadByte = (i >= length - effective) ? 1 : 0  (branchless)
                int diff = i - (length - effective);
                int shouldBePadByte = ((~diff) >> 31) & 1; // 1 iff diff >= 0

                // matches = (input[i] == padLen) ? 1 : 0  (branchless)
                int xor = input[i] ^ padLen;
                int matches = (((xor - 1) & ~xor) >> 31) & 1; // 1 iff xor == 0

                // Accumulate: valid &= (shouldBePadByte == 0) | (matches == 1)
                int constraint = (1 - shouldBePadByte) | matches;
                valid &= constraint;
            }

            if (valid == 0)
                throw new CryptographicException("Invalid PKCS#7 padding.");

            return input.Slice(0, length - padLen).ToArray();
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