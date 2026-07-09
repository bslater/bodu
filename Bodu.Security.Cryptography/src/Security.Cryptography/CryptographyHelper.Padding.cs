// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptographyHelper.Padding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal static partial class CryptographyHelper
{
    /// <summary>
    /// Removes padding from a block and returns the depadded data as a newly allocated array.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingMode" /> that was applied to <paramref name="block" />.</param>
    /// <param name="blockSize">
    /// The block size, in bits, used when the input was padded. Must be a positive multiple of 8.
    /// </param>
    /// <param name="block">The input buffer containing the padded block or blocks.</param>
    /// <param name="offset">The zero-based offset in <paramref name="block" /> at which the padded data begins.</param>
    /// <param name="count">
    /// The number of bytes to read from <paramref name="block" /> starting at <paramref name="offset" />.
    /// </param>
    /// <returns>A newly allocated <see cref="byte" /> array containing the input data with padding removed.</returns>
    /// <exception cref="CryptographicException">
    /// The padding is invalid, the specified range is not a positive multiple of <paramref name="blockSize" /> / 8, or
    /// the padding mode is unsupported.
    /// </exception>
    public static byte[] DepadBlock(PaddingMode padding, int blockSize, byte[] block, int offset, int count)
    {
        byte[] temp = new byte[count];
        int written = DepadBlock(padding, blockSize, new ReadOnlySpan<byte>(block, offset, count), temp);
        byte[] result = new byte[written];
        Buffer.BlockCopy(temp, 0, result, 0, written);
        return result;
    }

    /// <summary>
    /// Removes padding from a block and writes the depadded data into the specified destination span.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingMode" /> applied to <paramref name="source" />.</param>
    /// <param name="blockSize">
    /// The block size, in bits, used when the input was padded. Must be a positive multiple of 8.
    /// </param>
    /// <param name="source">
    /// The padded input data. Its byte length must be a positive multiple of <paramref name="blockSize" /> / 8.
    /// </param>
    /// <param name="destination">The destination span that receives the depadded data.</param>
    /// <returns>
    /// The number of bytes written to <paramref name="destination" /> after padding has been removed.
    /// </returns>
    /// <exception cref="CryptographicException">
    /// The padding is invalid, <paramref name="source" /> byte length is not a positive multiple of
    /// <paramref name="blockSize" /> / 8, or the padding mode is unsupported.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The uniform-byte pad check is constant-time in content: it scans the whole candidate pad region without a
    /// data-dependent early exit, so it does not leak the position of the first bad byte through timing or control
    /// flow.
    /// </para>
    /// <para>
    /// This is <b>not</b>, on its own, a full padding-oracle defence — the returned length still varies with the
    /// declared pad count, and an invalid block signals through a thrown <see cref="CryptographicException" />. Callers
    /// must <b>authenticate the ciphertext</b> (a MAC verified with
    /// <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />, or an AEAD mode)
    /// <b>before</b> depadding, so a padding failure is never observable to an attacker. Prefer the AEAD modes for new
    /// designs; raw CBC with strippable padding requires caller-supplied authentication.
    /// </para>
    /// </remarks>
    public static int DepadBlock(
        PaddingMode padding,
        int blockSize,
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(blockSize, 0);

        int size = blockSize / 8;
        CryptographyThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(source, size);

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
                    string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_PropertyValue, nameof(SymmetricAlgorithm.Padding)));
        }

        int padCount = source[^1];
        if (padCount <= 0 || padCount > size)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_Padding);

        ReadOnlySpan<byte> padRegion = source[^padCount..];

        if (padding == PaddingMode.PKCS7 && !IsUniformPadding(padRegion, (byte)padCount))
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_Padding);

        if (padding == PaddingMode.ANSIX923 && !IsUniformPadding(padRegion[..^1], 0x00))
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_Padding);

        int unpadded = count - padCount;
        source[..unpadded].CopyTo(destination);
        return unpadded;
    }

    /// <summary>
    /// Applies the specified padding mode to a block and returns the padded data as a newly allocated array.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingMode" /> to apply.</param>
    /// <param name="blockSize">The block size in bits used to align the output.</param>
    /// <param name="block">The input buffer containing the data to pad.</param>
    /// <param name="offset">The zero-based offset in <paramref name="block" /> at which to begin reading.</param>
    /// <param name="count">
    /// The number of bytes to read from <paramref name="block" /> starting at <paramref name="offset" />.
    /// </param>
    /// <returns>A newly allocated <see cref="byte" /> array containing the input data with padding applied.</returns>
    /// <exception cref="CryptographicException">
    /// The padding mode is invalid, or <paramref name="padding" /> is <see cref="PaddingMode.None" /> and the input
    /// length is not block-aligned.
    /// </exception>
    public static byte[] PadBlock(PaddingMode padding, int blockSize, byte[] block, int offset, int count)
    {
        ThrowHelper.ThrowIfLessThan(blockSize, 1);
        int size = blockSize / 8;
        byte[] result = new byte[count + size];
        int written = PadBlock(padding, blockSize, new ReadOnlySpan<byte>(block, offset, count), result);
        Array.Resize(ref result, written);
        return result;
    }

    /// <summary>
    /// Applies the specified padding mode to a block and writes the padded result into the destination span.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingMode" /> to apply.</param>
    /// <param name="blockSize">The block size in bits used to align the output.</param>
    /// <param name="source">The input data to pad.</param>
    /// <param name="destination">The destination span that receives the padded result.</param>
    /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="destination" /> is too small to hold the padded result.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// The padding mode is invalid, or <paramref name="padding" /> is <see cref="PaddingMode.None" /> and the input
    /// length is not a multiple of <paramref name="blockSize" />.
    /// </exception>
    public static int PadBlock(
        PaddingMode padding,
        int blockSize,
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
                    string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_PropertyValue, nameof(SymmetricAlgorithm.Padding)));
        }

        ThrowHelper.ThrowIfLessThan(blockSize, 1);

        int size = blockSize / 8;
        if (padding == PaddingMode.None && source.Length % size != 0)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_PaddingModeNoneInputNotAligned);

        int padCount = size - (source.Length % size);
        if (padCount == size && (padding == PaddingMode.None || padding == PaddingMode.Zeros))
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
    /// Attempts to remove padding from the specified input buffer using the given padding mode.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingMode" /> to validate and remove.</param>
    /// <param name="blockSize">The block size in bits used when the input was padded.</param>
    /// <param name="source">The padded input buffer.</param>
    /// <param name="destination">The destination span that receives the depadded data.</param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written to <paramref name="destination" />.
    /// </param>
    /// <returns><see langword="true" /> if depadding was successful; otherwise, <see langword="false" />.</returns>
    public static bool TryDepadBlock(
        PaddingMode padding,
        int blockSize,
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        out int bytesWritten)
    {
        try
        {
            bytesWritten = DepadBlock(padding, blockSize, source, destination);
            return true;
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to apply padding to the specified input buffer using the given padding mode.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingMode" /> to apply.</param>
    /// <param name="blockSize">The block size in bits used to align the output.</param>
    /// <param name="source">The input buffer to pad.</param>
    /// <param name="destination">The destination span that receives the padded data.</param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written to <paramref name="destination" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if padding was successfully applied; otherwise, <see langword="false" />.
    /// </returns>
    public static bool TryPadBlock(
        PaddingMode padding,
        int blockSize,
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        out int bytesWritten)
    {
        try
        {
            bytesWritten = PadBlock(padding, blockSize, source, destination);
            return true;
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    /// <summary>
    /// Removes padding from a block using the extended <see cref="PaddingModeKind" /> selector and returns the depadded
    /// data as a newly allocated array.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingModeKind" /> that was applied to <paramref name="block" />.</param>
    /// <param name="blockSize">
    /// The block size, in bits, used when the input was padded. Must be a positive multiple of 8.
    /// </param>
    /// <param name="block">The input buffer containing the padded block or blocks.</param>
    /// <param name="offset">The zero-based offset in <paramref name="block" /> at which the padded data begins.</param>
    /// <param name="count">
    /// The number of bytes to read from <paramref name="block" /> starting at <paramref name="offset" />.
    /// </param>
    /// <returns>A newly allocated <see cref="byte" /> array containing the input data with padding removed.</returns>
    /// <exception cref="CryptographicException">
    /// The padding is invalid, the specified range is not a positive multiple of <paramref name="blockSize" />, or the
    /// padding mode is unsupported.
    /// </exception>
    public static byte[] DepadBlock(PaddingModeKind padding, int blockSize, byte[] block, int offset, int count)
    {
        if (padding == PaddingModeKind.ISO7816_4)
        {
            ThrowHelper.ThrowIfLessThanOrEqual(blockSize, 0);
            var strategy = new Iso7816_4Padding();
            return strategy.Unpad(new ReadOnlySpan<byte>(block, offset, count), blockSize);
        }

        return DepadBlock((PaddingMode)padding, blockSize, block, offset, count);
    }

    /// <summary>
    /// Removes padding from a block using the extended <see cref="PaddingModeKind" /> selector and writes the depadded
    /// data into the specified destination span.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingModeKind" /> applied to <paramref name="source" />.</param>
    /// <param name="blockSize">
    /// The block size, in bits, used when the input was padded. Must be a positive multiple of 8.
    /// </param>
    /// <param name="source">
    /// The padded input data. Its byte length must be a positive multiple of <paramref name="blockSize" /> / 8.
    /// </param>
    /// <param name="destination">The destination span that receives the depadded data.</param>
    /// <returns>
    /// The number of bytes written to <paramref name="destination" /> after padding has been removed.
    /// </returns>
    /// <exception cref="CryptographicException">
    /// The padding is invalid, <paramref name="source" /> byte length is not a positive multiple of
    /// <paramref name="blockSize" /> / 8, or the padding mode is unsupported.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when an argument does not satisfy the method preconditions.
    /// </exception>
    public static int DepadBlock(
        PaddingModeKind padding,
        int blockSize,
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        if (padding == PaddingModeKind.ISO7816_4)
        {
            ThrowHelper.ThrowIfLessThanOrEqual(blockSize, 0);
            CryptographyThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(source, blockSize / 8);

            var strategy = new Iso7816_4Padding();
            byte[] unpadded = strategy.Unpad(source, blockSize);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, 0, unpadded.Length);
            unpadded.AsSpan().CopyTo(destination);
            return unpadded.Length;
        }

        return DepadBlock((PaddingMode)padding, blockSize, source, destination);
    }

    /// <summary>
    /// Applies the specified <see cref="PaddingModeKind" /> to a block and returns the padded data as a newly allocated
    /// array.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingModeKind" /> to apply.</param>
    /// <param name="blockSize">The block size in bits used to align the output.</param>
    /// <param name="block">The input buffer containing the data to pad.</param>
    /// <param name="offset">The zero-based offset in <paramref name="block" /> at which to begin reading.</param>
    /// <param name="count">
    /// The number of bytes to read from <paramref name="block" /> starting at <paramref name="offset" />.
    /// </param>
    /// <returns>A newly allocated <see cref="byte" /> array containing the input data with padding applied.</returns>
    /// <exception cref="CryptographicException">
    /// The padding mode is invalid, or <paramref name="padding" /> is <see cref="PaddingModeKind.None" /> and the input
    /// length is not block-aligned.
    /// </exception>
    public static byte[] PadBlock(PaddingModeKind padding, int blockSize, byte[] block, int offset, int count)
    {
        if (padding == PaddingModeKind.ISO7816_4)
        {
            ThrowHelper.ThrowIfLessThan(blockSize, 1);
            var strategy = new Iso7816_4Padding();
            return strategy.Pad(new ReadOnlySpan<byte>(block, offset, count), blockSize);
        }

        return PadBlock((PaddingMode)padding, blockSize, block, offset, count);
    }

    /// <summary>
    /// Applies the specified <see cref="PaddingModeKind" /> to a block and writes the padded result into the
    /// destination span.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingModeKind" /> to apply.</param>
    /// <param name="blockSize">The block size in bits used to align the output.</param>
    /// <param name="source">The input data to pad.</param>
    /// <param name="destination">The destination span that receives the padded result.</param>
    /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="destination" /> is too small to hold the padded result.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// The padding mode is invalid, or <paramref name="padding" /> is <see cref="PaddingModeKind.None" /> and the input
    /// length is not a multiple of <paramref name="blockSize" />.
    /// </exception>
    public static int PadBlock(
        PaddingModeKind padding,
        int blockSize,
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        if (padding == PaddingModeKind.ISO7816_4)
        {
            ThrowHelper.ThrowIfLessThan(blockSize, 1);

            var strategy = new Iso7816_4Padding();
            byte[] padded = strategy.Pad(source, blockSize);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, 0, padded.Length);
            padded.AsSpan().CopyTo(destination);
            return padded.Length;
        }

        return PadBlock((PaddingMode)padding, blockSize, source, destination);
    }

    /// <summary>
    /// Attempts to remove padding from the specified input buffer using the given <see cref="PaddingModeKind" />.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingModeKind" /> to validate and remove.</param>
    /// <param name="blockSize">The block size in bits used when the input was padded.</param>
    /// <param name="source">The padded input buffer.</param>
    /// <param name="destination">The destination span that receives the depadded data.</param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written to <paramref name="destination" />.
    /// </param>
    /// <returns><see langword="true" /> if depadding was successful; otherwise, <see langword="false" />.</returns>
    public static bool TryDepadBlock(
        PaddingModeKind padding,
        int blockSize,
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        out int bytesWritten)
    {
        try
        {
            bytesWritten = DepadBlock(padding, blockSize, source, destination);
            return true;
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to apply padding to the specified input buffer using the given <see cref="PaddingModeKind" />.
    /// </summary>
    /// <param name="padding">The <see cref="PaddingModeKind" /> to apply.</param>
    /// <param name="blockSize">The block size in bits used to align the output.</param>
    /// <param name="source">The input buffer to pad.</param>
    /// <param name="destination">The destination span that receives the padded data.</param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written to <paramref name="destination" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if padding was successfully applied; otherwise, <see langword="false" />.
    /// </returns>
    public static bool TryPadBlock(
        PaddingModeKind padding,
        int blockSize,
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        out int bytesWritten)
    {
        try
        {
            bytesWritten = PadBlock(padding, blockSize, source, destination);
            return true;
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    /// <summary>
    /// Checks whether a span consists entirely of a single repeated byte value.
    /// </summary>
    /// <param name="span">The span to validate.</param>
    /// <param name="expected">The expected uniform byte value.</param>
    /// <returns>
    /// <see langword="true" /> if every byte in <paramref name="span" /> equals <paramref name="expected" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    private static bool IsUniformPadding(ReadOnlySpan<byte> span, byte expected)
    {
        // Accumulate a difference mask over the entire candidate pad region with no data-dependent early exit, so
        // the check's duration and control flow do not reveal where the first mismatched byte sits. Returning a
        // single boolean at the end keeps pad validation from leaking a byte-position timing signal (the classic
        // CBC padding-oracle vector). The scanned region is at most one block, so the full pass is negligible.
        int diff = 0;
        foreach (byte b in span)
            diff |= b ^ expected;

        return diff == 0;
    }
}
