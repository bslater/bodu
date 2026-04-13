// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoUtilities.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System.Security.Cryptography;

    /// <summary>
    /// Provides cryptographic utility functions for applying and removing block padding.
    /// </summary>
    public static partial class CryptoHelpers
    {
        /// <summary>
        /// Removes padding from a block and returns a newly allocated array.
        /// </summary>
        /// <param name="padding">The padding mode used in the block.</param>
        /// <param name="blockSizeBytes">The block size in bytes.</param>
        /// <param name="block">The input padded block.</param>
        /// <param name="offset">Offset in the input buffer.</param>
        /// <param name="count">Number of bytes to process.</param>
        /// <returns>A new byte array with padding removed.</returns>
        public static byte[] DepadBlock(PaddingMode padding, int blockSizeBytes, byte[] block, int offset, int count)
        {
            byte[] temp = new byte[count];
            int written = DepadBlock(padding, blockSizeBytes, new ReadOnlySpan<byte>(block, offset, count), temp);
            byte[] result = new byte[written];
            Buffer.BlockCopy(temp, 0, result, 0, written);
            return result;
        }

        /// <summary>
        /// Removes padding from a block and writes the depadded data into the destination span.
        /// </summary>
        /// <param name="padding">The padding mode applied to the input data.</param>
        /// <param name="blockSizeBytes">The block size in bytes for validation.</param>
        /// <param name="source">The padded input data.</param>
        /// <param name="destination">The destination span to receive depadded data.</param>
        /// <returns>The number of unpadded bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="CryptographicException">
        /// Thrown if the padding is invalid, the source is not block-aligned, or the padding mode is unsupported.
        /// </exception>
        public static int DepadBlock(
            PaddingMode padding,
            int blockSizeBytes,
            ReadOnlySpan<byte> source,
            Span<byte> destination)
        {
            ThrowHelper.ThrowIfLessThanOrEqual(blockSizeBytes, 0);
            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(source, blockSizeBytes);

            int count = source.Length;

            switch (padding)
            {
                case PaddingMode.None:
                case PaddingMode.Zeros:
                    source.CopyTo(destination);
                    return count;

                case PaddingMode.PKCS7:
                case PaddingMode.ANSIX923:
                case PaddingMode.ISO10126:
                    break;

                default:
                    throw new CryptographicException(
                        string.Format(ResourceStrings.CryptographicException_InvalidPropertyValue, nameof(SymmetricAlgorithm.Padding)));
            }

            int padCount = source[^1];
            if (padCount <= 0 || padCount > blockSizeBytes)
                throw new CryptographicException(ResourceStrings.CryptographicException_InvalidPadding);

            ReadOnlySpan<byte> padRegion = source[^padCount..];

            if (padding == PaddingMode.PKCS7 && !IsUniformPadding(padRegion, (byte)padCount))
                throw new CryptographicException(ResourceStrings.CryptographicException_InvalidPadding);

            if (padding == PaddingMode.ANSIX923 && !IsUniformPadding(padRegion[..^1], 0x00))
                throw new CryptographicException(ResourceStrings.CryptographicException_InvalidPadding);

            int unpadded = count - padCount;
            source.Slice(0, unpadded).CopyTo(destination);
            return unpadded;
        }

        /// <summary>
        /// Applies padding to a block and returns a newly allocated array.
        /// </summary>
        /// <param name="padding">The padding mode to apply.</param>
        /// <param name="blockSizeBytes">The block size in bytes.</param>
        /// <param name="block">The input buffer to pad.</param>
        /// <param name="offset">The offset within the buffer to start reading.</param>
        /// <param name="count">The number of bytes to read from the buffer.</param>
        /// <returns>A new padded byte array.</returns>
        public static byte[] PadBlock(PaddingMode padding, int blockSizeBytes, byte[] block, int offset, int count)
        {
            ThrowHelper.ThrowIfLessThan(blockSizeBytes, 1);
            byte[] result = new byte[count + blockSizeBytes];
            int written = PadBlock(padding, blockSizeBytes, new ReadOnlySpan<byte>(block, offset, count), result);
            Array.Resize(ref result, written);
            return result;
        }

        /// <summary>
        /// Applies padding to a block using the specified padding mode and writes to the destination span.
        /// </summary>
        /// <param name="padding">Padding mode to apply.</param>
        /// <param name="blockSizeBytes">The block size in bytes.</param>
        /// <param name="source">Input data to pad.</param>
        /// <param name="destination">Destination span for the padded result.</param>
        /// <returns>Total bytes written to the destination span.</returns>
        /// <exception cref="ArgumentException">Thrown when the destination span is too small.</exception>
        /// <exception cref="CryptographicException">Thrown if the padding mode is invalid or the input is not aligned.</exception>
        public static int PadBlock(
            PaddingMode padding,
            int blockSizeBytes,
            ReadOnlySpan<byte> source,
            Span<byte> destination)
        {
            switch (padding)
            {
                case PaddingMode.PKCS7:
                case PaddingMode.ANSIX923:
                case PaddingMode.ISO10126:
                case PaddingMode.Zeros:
                case PaddingMode.None:
                    break;

                default:
                    throw new CryptographicException(
                        string.Format(ResourceStrings.CryptographicException_InvalidPropertyValue, nameof(SymmetricAlgorithm.Padding)));
            }

            ThrowHelper.ThrowIfLessThan(blockSizeBytes, 1);

            if (padding == PaddingMode.None && source.Length % blockSizeBytes != 0)
                throw new CryptographicException(ResourceStrings.CryptographicException_PaddingModeNone_InputNotAligned);

            int padCount = blockSizeBytes - (source.Length % blockSizeBytes);
            if (padCount == blockSizeBytes && (padding == PaddingMode.None || padding == PaddingMode.Zeros))
                padCount = 0;

            int totalLen = source.Length + padCount;
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, source.Length, padCount);

            source.CopyTo(destination);
            Span<byte> padSpan = destination.Slice(source.Length, padCount);

            switch (padding)
            {
                case PaddingMode.None:
                case PaddingMode.Zeros:
                    padSpan.Clear();
                    break;

                case PaddingMode.PKCS7:
                    padSpan.Fill((byte)padCount);
                    break;

                case PaddingMode.ANSIX923:
                    padSpan.Clear();
                    padSpan[^1] = (byte)padCount;
                    break;

                case PaddingMode.ISO10126:
                    FillWithRandomNonZeroBytes(padSpan[..^1]);
                    padSpan[^1] = (byte)padCount;
                    break;
            }

            return totalLen;
        }

        /// <summary>
        /// Attempts to remove padding from the given input buffer using the specified mode.
        /// </summary>
        /// <param name="padding">The padding mode to validate and remove.</param>
        /// <param name="blockSizeBytes">The block size in bytes.</param>
        /// <param name="source">The padded input buffer.</param>
        /// <param name="destination">The destination span that receives the depadded data.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination" />.</param>
        /// <returns><c>true</c> if depadding was successful; otherwise, <c>false</c>.</returns>
        public static bool TryDepadBlock(
            PaddingMode padding,
            int blockSizeBytes,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            try
            {
                bytesWritten = DepadBlock(padding, blockSizeBytes, source, destination);
                return true;
            }
            catch
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <summary>
        /// Attempts to apply padding to the given input buffer using the specified mode.
        /// </summary>
        /// <param name="padding">The padding mode to apply.</param>
        /// <param name="blockSizeBytes">The block size in bytes.</param>
        /// <param name="source">The input buffer to pad.</param>
        /// <param name="destination">The destination span that receives the padded data.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination" />.</param>
        /// <returns><c>true</c> if padding was successfully applied; otherwise, <c>false</c>.</returns>
        public static bool TryPadBlock(
            PaddingMode padding,
            int blockSizeBytes,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            try
            {
                bytesWritten = PadBlock(padding, blockSizeBytes, source, destination);
                return true;
            }
            catch
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <summary>
        /// Checks whether a span consists entirely of a single repeated value.
        /// </summary>
        /// <param name="span">The span to validate.</param>
        /// <param name="expected">The expected uniform byte value.</param>
        /// <returns><c>true</c> if all bytes match the expected value; otherwise, <c>false</c>.</returns>
        private static bool IsUniformPadding(ReadOnlySpan<byte> span, byte expected)
        {
            foreach (byte b in span)
            {
                if (b != expected)
                    return false;
            }

            return true;
        }
    }
}