// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptographyThrowHelper.ThrowHelper.CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1117:Parameters should be on same line or separate lines", Justification = "The [CallerArgumentExpression] parameter shares a line with the preceding parameter to keep the logical parameter grouping compact and to mirror the netstandard sibling file.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0011:Add braces", Justification = "Single-statement throw guards are intentionally brace-free to maintain density; these are pure guard-clause helpers that do not benefit from braces.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1001:Add braces (when expression spans over multiple lines)", Justification = "Single-statement throw guards are intentionally brace-free to maintain density; these are pure guard-clause helpers that do not benefit from braces.")]
internal static partial class CryptographyThrowHelper
{
    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> if associated data has already been processed.
    /// </summary>
    /// <param name="alreadyProcessed">
    /// <see langword="true" /> if associated data was already supplied; <see langword="false" /> otherwise.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="alreadyProcessed" /> is <see langword="true" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAssociatedDataAlreadyProcessed(bool alreadyProcessed)
    {
        if (alreadyProcessed)
            throw new InvalidOperationException(CryptoResourceStrings.Crypt_Invalid_AssociatedDataAlreadyProcessed);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if associated data has not yet been processed.
    /// </summary>
    /// <param name="alreadyProcessed">
    /// <see langword="true" /> if associated data has been supplied; <see langword="false" /> if it has not.
    /// </param>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="alreadyProcessed" /> is <see langword="false" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAssociatedDataNotProcessed(bool alreadyProcessed)
    {
        if (!alreadyProcessed)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AssociatedDataNotProcessed);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if <paramref name="input" /> and <paramref name="output" />
    /// partially overlap in memory.
    /// </summary>
    /// <param name="input">The input span being read by the transform.</param>
    /// <param name="output">The output span being written by the transform.</param>
    /// <param name="allowExactInPlace">
    /// When <see langword="true" /> (the default) the spans may alias exactly — same start address and same length — so
    /// that the caller can perform an in-place transform with a single buffer. When <see langword="false" />, any
    /// overlap (including exact aliasing) is rejected.
    /// </param>
    /// <exception cref="CryptographicException">
    /// The spans overlap in a way that is not safe for forward byte-by-byte processing: either a partial overlap, or
    /// exact aliasing when <paramref name="allowExactInPlace" /> is <see langword="false" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Cryptographic transforms read input sequentially and write output sequentially. Exact in-place aliasing is safe
    /// for stream and block ciphers because each output byte is computed and written before the next input byte is
    /// read. Partial overlap, however, lets a write into not-yet-read input clobber the keystream / chaining input,
    /// producing a silently corrupted result.
    /// </para>
    /// <para>
    /// The check is short-circuited when either span is empty: zero-length operations cannot overlap meaningfully.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfInvalidOverlap(ReadOnlySpan<byte> input, Span<byte> output, bool allowExactInPlace = true)
    {
        if (input.IsEmpty || output.IsEmpty)
            return;

        if (!input.Overlaps(output, out var elementOffset))
            return;

        if (allowExactInPlace && elementOffset == 0 && input.Length == output.Length)
            return;

        throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_PartialBufferOverlap);
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> if the transform has already completed.
    /// </summary>
    /// <param name="completed">
    /// <see langword="true" /> if the transform has already produced its output; <see langword="false" /> otherwise.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="completed" /> is <see langword="true" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAlreadyCompleted(bool completed)
    {
        if (completed)
            throw new InvalidOperationException(CryptoResourceStrings.Op_Invalid_TransformAlreadyFinalized);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="output" /> is smaller than
    /// <paramref name="required" /> bytes.
    /// </summary>
    /// <param name="output">The output buffer to validate.</param>
    /// <param name="required">The minimum number of bytes the buffer must hold.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">Thrown when <c>output.Length &lt; required</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfOutputBufferTooSmall(
        Span<byte> output, int required,
        [CallerArgumentExpression(nameof(output))] string? paramName = null)
    {
        if (output.Length < required)
        {
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_OutputBufferTooSmall, required),
                paramName);
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="input" /> is shorter than
    /// <paramref name="tagSize" /> bytes, meaning it cannot contain a complete authentication tag.
    /// </summary>
    /// <param name="input">The ciphertext-with-tag buffer to validate.</param>
    /// <param name="tagSize">The required tag size in bytes.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">Thrown when <c>input.Length &lt; tagSize</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCiphertextTooShort(
        ReadOnlySpan<byte> input, int tagSize,
        [CallerArgumentExpression(nameof(input))] string? paramName = null)
    {
        if (input.Length < tagSize)
        {
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_CiphertextTooShort, tagSize),
                paramName);
        }
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if the span length is not a valid multiple of
    /// <paramref name="divisor" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span whose length is validated.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="throwIfZero">
    /// <see langword="true" /> to treat an empty span as invalid; <see langword="false" /> to allow an empty span.
    /// </param>
    /// <param name="paramName">The name of the span parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="divisor" /> is zero or negative.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="span" /> is empty and <paramref name="throwIfZero" /> is <see langword="true" />, or
    /// when the span length is not evenly divisible by <paramref name="divisor" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthNotPositiveMultipleOf<T>(
        ReadOnlySpan<T> span, int divisor, bool throwIfZero = true,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        ThrowHelper.ThrowIfZeroOrNegative(divisor);

        if ((throwIfZero && span.Length == 0) || span.Length % divisor != 0)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_InputLengthBlockMultiple, divisor),
                paramName);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if the specified value is not a positive multiple of
    /// <paramref name="divisor" />.
    /// </summary>
    /// <typeparam name="T">A binary integer type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="divisor" /> is zero or negative.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="value" /> is less than or equal to zero, or when <paramref name="value" /> is not
    /// evenly divisible by <paramref name="divisor" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotPositiveMultipleOf<T>(
        T value, T divisor,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IBinaryInteger<T>
    {
        if (divisor <= T.Zero)
            throw new ArgumentOutOfRangeException(nameof(divisor));

        if (value <= T.Zero || value % divisor != T.Zero)
            throw new CryptographicException(
                paramName!,
                string.Format(CryptoResourceStrings.Crypt_Invalid_HashSizePositiveMultipleOf, divisor));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, an
    /// <see cref="ArgumentException" /> if <paramref name="offset" /> or <paramref name="count" /> is out of range, or
    /// an <see cref="ArgumentException" /> if the segment they define exceeds the bounds of <paramref name="array" />.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based starting index within the array.</param>
    /// <param name="count">The number of elements to access from <paramref name="offset" />.</param>
    /// <param name="paramArrayName">The name of the array parameter. Supplied automatically by the compiler.</param>
    /// <param name="paramOffsetName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <param name="paramCountName">The name of the count parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is negative or exceeds <c>array.Length</c>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <c>index + count</c> exceeds <c>array.Length</c>.</exception>
    public static void ThrowIfArrayOffsetOrCountInvalid(
        Array array, int offset, int count,
        [CallerArgumentExpression(nameof(array))] string? paramArrayName = null,
        [CallerArgumentExpression(nameof(offset))] string? paramOffsetName = null,
        [CallerArgumentExpression(nameof(count))] string? paramCountName = null)
    {
        if (array is null)
        {
            throw new ArgumentNullException(paramArrayName);
        }

        if (offset < 0 || offset > array.Length)
        {
            throw new ArgumentOutOfRangeException(
                paramOffsetName,
                string.Format(ResourceStrings.Arg_Invalid_ArrayOffset, paramOffsetName));
        }

        if (count < 0 || count > array.Length)
        {
            throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_ArrayOffset, paramCountName),
                paramCountName);
        }

        if (count > array.Length - offset)
        {
            throw new ArgumentException(
                string.Format(
                    ResourceStrings.Arg_Invalid_ArrayOffsetOrLength,
                    paramOffsetName,
                    paramCountName,
                    paramArrayName));
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the specified hash size is not one of the permitted hash
    /// sizes.
    /// </summary>
    /// <param name="hashSize">The hash size, in bits, to validate.</param>
    /// <param name="permittedHashSizes">The set of valid hash sizes, in bits.</param>
    /// <param name="paramHashSizeName">
    /// The name of the hash-size parameter. Supplied automatically by the compiler.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="permittedHashSizes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="hashSize" /> is not present in <paramref name="permittedHashSizes" />.
    /// </exception>
    public static void ThrowIfInvalidHashSize(
        int hashSize,
        int[] permittedHashSizes,
        [CallerArgumentExpression(nameof(hashSize))] string? paramHashSizeName = null)
    {
        ThrowHelper.ThrowIfNull(permittedHashSizes);

        if (Array.IndexOf(permittedHashSizes, hashSize) == -1)
        {
            throw new ArgumentOutOfRangeException(
                paramHashSizeName,
                string.Format(
                    CryptoResourceStrings.Crypt_Invalid_HashSize,
                    hashSize,
                    string.Join(", ", permittedHashSizes)));
        }
    }

    /// <summary>
    /// Validates an initialization vector against the configured <paramref name="mode" />.
    /// </summary>
    /// <param name="iv">The initialization vector to validate, or <see langword="null" />.</param>
    /// <param name="mode">The configured cipher mode.</param>
    /// <param name="blockSizeBits">The required IV size, in bits, when the mode requires one.</param>
    /// <param name="legalBlockSizes">
    /// The legal block sizes (in bits) for the algorithm, used for error formatting.
    /// </param>
    /// <param name="paramName">The parameter name. Supplied automatically by the compiler.</param>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="iv" /> is <see langword="null" /> and <paramref name="mode" /> is not
    /// <see cref="CipherModeKind.ECB" />, or when <paramref name="iv" /> is non-null but
    /// <c>iv.Length * 8 != blockSizeBits</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidIVForMode(
        byte[]? iv,
        CipherModeKind mode,
        int blockSizeBits,
        KeySizes[] legalBlockSizes,
        [CallerArgumentExpression(nameof(iv))] string? paramName = null)
    {
        if (iv is null)
        {
            if (mode == CipherModeKind.ECB) return;
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_IVRequiredForMode);
        }

        ThrowHelper.ThrowIfNull(legalBlockSizes);

        if (iv.Length != blockSizeBits / 8)
            throw new CryptographicException(
                string.Format(
                    CryptoResourceStrings.Crypt_Invalid_IVSize,
                    iv.Length * 8,
                    CryptographyHelper.FormatLegalSizes(legalBlockSizes)));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the byte length of <paramref name="iv" /> does not equal
    /// <paramref name="blockSizeBits" /> / 8.
    /// </summary>
    /// <param name="iv">The initialization vector to validate.</param>
    /// <param name="blockSizeBits">The required IV size, in bits.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="iv" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <c>iv.Length * 8 != blockSizeBits</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIvLengthInvalid(
        byte[] iv, int blockSizeBits,
        [CallerArgumentExpression(nameof(iv))] string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(iv, paramName);
        if (iv.Length != blockSizeBits / 8)
        {
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Arg_Invalid_IvLength, iv.Length * 8, blockSizeBits),
                paramName);
        }
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if the byte length of <paramref name="key" />, expressed in bits,
    /// is not one of the values permitted by <paramref name="legalKeySizes" />.
    /// </summary>
    /// <param name="key">The key material to validate.</param>
    /// <param name="keySizeBits">The algorithm's currently configured key size, in bits.</param>
    /// <param name="legalKeySizes">The legal key sizes for the algorithm (in bits).</param>
    /// <param name="paramKeyName">The name of the key parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="legalKeySizes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown when <c>key.Length * 8</c> is not one of the values produced by <paramref name="legalKeySizes" />.
    /// </exception>
    public static void ThrowIfInvalidKeySize(
        byte[] key,
        int keySizeBits,
        KeySizes[] legalKeySizes,
        [CallerArgumentExpression(nameof(key))] string? paramKeyName = null)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(legalKeySizes);

        var keyBits = key.Length * 8;
        if (!IsValidSize(keyBits, legalKeySizes))
            throw new CryptographicException(
                string.Format(
                    CryptoResourceStrings.Crypt_Invalid_KeySize,
                    keyBits,
                    CryptographyHelper.FormatLegalSizes(legalKeySizes)),
                paramKeyName);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if the supplied nonce is <see langword="null" /> or does not have
    /// the exact required byte length.
    /// </summary>
    /// <param name="nonce">The nonce supplied as the algorithm IV.</param>
    /// <param name="expectedBytes">The required nonce length, in bytes.</param>
    /// <param name="paramName">The name of the nonce parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="nonce" /> is <see langword="null" /> or its length is not
    /// <paramref name="expectedBytes" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidNonceSize(
        byte[]? nonce, int expectedBytes,
        [CallerArgumentExpression(nameof(nonce))] string? paramName = null)
    {
        var actualBits = (nonce?.Length ?? 0) * 8;
        if (nonce is null || nonce.Length != expectedBytes)
        {
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_IVSize, actualBits, expectedBytes * 8),
                paramName);
        }
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if the byte length of <paramref name="tweak" /> does not equal
    /// <paramref name="tweakSizeBits" /> / 8.
    /// </summary>
    /// <param name="tweak">The tweak material to validate.</param>
    /// <param name="tweakSizeBits">The required tweak size, in bits.</param>
    /// <param name="legalTweakSizes">The legal tweak sizes for the algorithm (in bits).</param>
    /// <param name="paramTweakName">The name of the tweak parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tweak" /> or <paramref name="legalTweakSizes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">Thrown when <c>tweak.Length * 8 != tweakSizeBits</c>.</exception>
    public static void ThrowIfInvalidTweakSize(
        byte[] tweak,
        int tweakSizeBits,
        KeySizes[] legalTweakSizes,
        [CallerArgumentExpression(nameof(tweak))] string? paramTweakName = null)
    {
        ThrowHelper.ThrowIfNull(tweak);
        ThrowHelper.ThrowIfNull(legalTweakSizes);

        if (tweak.Length != tweakSizeBits / 8)
            throw new CryptographicException(
                string.Format(
                    CryptoResourceStrings.Crypt_Invalid_TweakSize,
                    tweak.Length * 8,
                    CryptographyHelper.FormatLegalSizes(legalTweakSizes)),
                paramTweakName);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if the byte length of <paramref name="value" /> does not equal
    /// <paramref name="blockSizeBits" /> / 8.
    /// </summary>
    /// <param name="value">The block-sized value to validate, such as an initialization vector.</param>
    /// <param name="blockSizeBits">The required block size, in bits.</param>
    /// <param name="legalBlockSizes">The legal block sizes for the algorithm (in bits).</param>
    /// <param name="paramValueName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="legalBlockSizes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">Thrown when <c>value.Length * 8 != blockSizeBits</c>.</exception>
    public static void ThrowIfInvalidBlockSize(
        byte[] value,
        int blockSizeBits,
        KeySizes[] legalBlockSizes,
        [CallerArgumentExpression(nameof(value))] string? paramValueName = null)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(legalBlockSizes);

        if (value.Length != blockSizeBits / 8)
            throw new CryptographicException(
                string.Format(
                    CryptoResourceStrings.Crypt_Invalid_BlockSize,
                    value.Length * 8,
                    CryptographyHelper.FormatLegalSizes(legalBlockSizes)),
                paramValueName);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> when <paramref name="success" /> is <see langword="false" />,
    /// indicating that <see cref="HashAlgorithm.TryComputeHash" /> failed because the destination buffer was too small.
    /// </summary>
    /// <param name="success">
    /// The result returned from <see cref="HashAlgorithm.TryComputeHash" /> or an equivalent call.
    /// </param>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="success" /> is <see langword="false" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfHashAlgorithmDestinationTooSmall(bool success)
    {
        if (!success)
            throw new CryptographicException(
                CryptoResourceStrings.Crypt_Invalid_HashAlgorithmDestinationBufferTooSmall);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> when <paramref name="hash" /> is <see langword="null" />,
    /// indicating that the hash algorithm completed without producing a hash value.
    /// </summary>
    /// <param name="hash">
    /// The hash value returned by the algorithm, or <see langword="null" /> if no value was produced.
    /// </param>
    /// <returns>The non-<see langword="null" /> <paramref name="hash" /> value.</returns>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="hash" /> is <see langword="null" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ThrowIfHashAlgorithmProducedNoValue(byte[]? hash) =>
        hash ?? throw new CryptographicException(
            CryptoResourceStrings.Crypt_Invalid_HashAlgorithmDidNotProduceValue);

    /// <summary>
    /// Returns <see langword="true" /> if <paramref name="sizeBits" /> falls inside any of the ranges supplied by
    /// <paramref name="legalSizes" />.
    /// </summary>
    /// <param name="sizeBits">The size to test, in bits.</param>
    /// <param name="legalSizes">The permitted size ranges.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="sizeBits" /> matches one of <paramref name="legalSizes" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    private static bool IsValidSize(int sizeBits, KeySizes[] legalSizes)
    {
        foreach (KeySizes range in legalSizes)
        {
            if (range.SkipSize == 0)
            {
                if (sizeBits == range.MinSize)
                    return true;
            }
            else if (sizeBits >= range.MinSize
                  && sizeBits <= range.MaxSize
                  && ((sizeBits - range.MinSize) % range.SkipSize) == 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the imported raw key material does not have the exact length
    /// required by the algorithm.
    /// </summary>
    /// <param name="key">The raw key bytes to validate.</param>
    /// <param name="expectedLength">The required key length, in bytes.</param>
    /// <param name="keyDescription">
    /// A short label for the key kind used in the exception message, such as <c>"X25519 private"</c>.
    /// </param>
    /// <param name="paramName">The name of the key parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">Thrown when <c>key.Length != expectedLength</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidRawKeyLength(
        ReadOnlySpan<byte> key, int expectedLength, string keyDescription,
        [CallerArgumentExpression(nameof(key))] string? paramName = null)
    {
        if (key.Length != expectedLength)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_Invalid_RawKeyLength,
                    keyDescription,
                    expectedLength),
                paramName);
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if a caller-supplied destination span does not have the exact length
    /// the operation writes.
    /// </summary>
    /// <param name="destination">The destination span to validate.</param>
    /// <param name="expectedLength">The required destination length, in bytes.</param>
    /// <param name="paramName">The name of the destination parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">Thrown when <c>destination.Length != expectedLength</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidDestinationLength(
        Span<byte> destination, int expectedLength,
        [CallerArgumentExpression(nameof(destination))] string? paramName = null)
    {
        if (destination.Length != expectedLength)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_Invalid_DestinationLength,
                    expectedLength),
                paramName);
        }
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if an asymmetric operation requires private key material that is
    /// not present on the instance.
    /// </summary>
    /// <param name="hasPrivateKey">
    /// <see langword="true" /> if private key material is present; <see langword="false" /> otherwise.
    /// </param>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="hasPrivateKey" /> is <see langword="false" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNoPrivateKey(bool hasPrivateKey)
    {
        if (!hasPrivateKey)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_NoPrivateKey);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if an asymmetric operation requires public key material that is
    /// not present on the instance.
    /// </summary>
    /// <param name="hasPublicKey">
    /// <see langword="true" /> if public key material is present; <see langword="false" /> otherwise.
    /// </param>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="hasPublicKey" /> is <see langword="false" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNoPublicKey(bool hasPublicKey)
    {
        if (!hasPublicKey)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_NoPublicKey);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when an <see cref="IBlockCipher" /> does not have the block size
    /// required by a cipher mode.
    /// </summary>
    /// <param name="cipher">The block cipher supplied to the mode.</param>
    /// <param name="requiredBlockSizeBits">The block size, in bits, the mode requires.</param>
    /// <param name="role">A short label identifying the mode and cipher role for the exception message.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="cipher" /> does not report <paramref name="requiredBlockSizeBits" />.
    /// </exception>
    public static void ThrowIfBlockSizeNotEqualTo(
        IBlockCipher cipher, int requiredBlockSizeBits, string role,
        [CallerArgumentExpression(nameof(cipher))] string? paramName = null)
    {
        if (cipher.BlockSize != requiredBlockSizeBits)
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_Invalid_CipherBlockSizeRequired,
                    role,
                    requiredBlockSizeBits / 8),
                paramName);
    }
}
