// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpers.ThrowHelper.NetStandard.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if NETSTANDARD2_0_OR_GREATER
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1117:Parameters should be on same line or separate lines")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0011:Add braces")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1001:Add braces (when expression spans over multiple lines)")]
internal static partial class CryptoHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAssociatedDataAlreadyProcessed(bool alreadyProcessed)
    {
        if (alreadyProcessed)
            throw new InvalidOperationException(CryptoResourceStrings.Crypt_Invalid_AssociatedDataAlreadyProcessed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAssociatedDataNotProcessed(bool alreadyProcessed)
    {
        if (!alreadyProcessed)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AssociatedDataNotProcessed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAlreadyCompleted(bool completed)
    {
        if (completed)
            throw new InvalidOperationException(CryptoResourceStrings.Op_Invalid_TransformAlreadyFinalized);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfOutputBufferTooSmall(Span<byte> output, int required, string? paramName = null)
    {
        if (output.Length < required)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_OutputBufferTooSmall, required),
                paramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCiphertextTooShort(ReadOnlySpan<byte> input, int tagSize, string? paramName = null)
    {
        if (input.Length < tagSize)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_CiphertextTooShort, tagSize),
                paramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthNotPositiveMultipleOf<T>(ReadOnlySpan<T> span, int divisor, bool throwIfZero = true, string? paramName = null)
    {
        ThrowHelper.ThrowIfZeroOrNegative(divisor);

        if ((throwIfZero && span.Length == 0) || span.Length % divisor != 0)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_InputLengthBlockMultiple, divisor),
                paramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotPositiveMultipleOf<T>(T value, T divisor, string? paramName = null)
        where T : IBinaryInteger<T>
    {
        if (divisor <= T.Zero)
            throw new ArgumentOutOfRangeException(nameof(divisor));

        if (value <= T.Zero || value % divisor != T.Zero)
            throw new CryptographicException(
                paramName,
                string.Format(CryptoResourceStrings.Crypt_Invalid_HashSizePositiveMultipleOf, divisor));
    }

    public static void ThrowIfArrayOffsetOrCountInvalid(Array array, int offset, int count, string? paramArrayName = null, string? paramOffsetName = null, string? paramCountName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramArrayName);

        if (offset < 0 || offset > array.Length)
            throw new ArgumentOutOfRangeException(
                paramOffsetName,
                string.Format(ResourceStrings.Arg_Invalid_ArrayOffset, paramOffsetName));

        if (count < 0 || count > array.Length)
            throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_ArrayOffset, paramCountName),
                paramCountName);

        if (count > array.Length - offset)
            throw new ArgumentException(
                string.Format(
                    ResourceStrings.Arg_Invalid_ArrayOffsetOrLength,
                    paramOffsetName,
                    paramCountName,
                    paramArrayName));
    }

    public static void ThrowIfInvalidHashSize(int hashSize, int[] permittedHashSizes, string? paramHashSizeName = null)
    {
        ThrowHelper.ThrowIfNull(permittedHashSizes);

        if (Array.IndexOf(permittedHashSizes, hashSize) == -1)
            throw new ArgumentOutOfRangeException(
                paramHashSizeName,
                string.Format(
                    CryptoResourceStrings.Crypt_Invalid_HashSize,
                    hashSize,
                    string.Join(", ", permittedHashSizes)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidIVForMode(byte[]? iv, CipherModeKind mode, int blockSizeBits, KeySizes[] legalBlockSizes, string? paramName = null)
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
                    CryptoHelpers.FormatLegalSizes(legalBlockSizes)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIvLengthInvalid(byte[] iv, int blockSizeBits, string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(iv, paramName);
        if (iv.Length != blockSizeBits / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Arg_Invalid_IvLength, iv.Length * 8, blockSizeBits),
                paramName);
    }

    public static void ThrowIfInvalidKeySize(byte[] key, int keySizeBits, KeySizes[] legalKeySizes, string? paramKeyName = null)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(legalKeySizes);

        var keyBits = key.Length * 8;
        if (!IsValidSize(keyBits, legalKeySizes))
            throw new CryptographicException(
                string.Format(
                    CryptoResourceStrings.Crypt_Invalid_KeySize,
                    keyBits,
                    CryptoHelpers.FormatLegalSizes(legalKeySizes)),
                paramKeyName);
    }

    public static void ThrowIfInvalidTweakSize(byte[] tweak, int tweakSizeBits, KeySizes[] legalTweakSizes, string? paramTweakName = null)
    {
        ThrowHelper.ThrowIfNull(tweak);
        ThrowHelper.ThrowIfNull(legalTweakSizes);

        if (tweak.Length != tweakSizeBits / 8)
            throw new CryptographicException(
                string.Format(
                    CryptoResourceStrings.Crypt_Invalid_TweakSize,
                    tweak.Length * 8,
                    CryptoHelpers.FormatLegalSizes(legalTweakSizes)),
                paramTweakName);
    }

    public static void ThrowIfInvalidBlockSize(byte[] value, int blockSizeBits, KeySizes[] legalBlockSizes, string? paramValueName = null)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(legalBlockSizes);

        if (value.Length != blockSizeBits / 8)
            throw new CryptographicException(
                string.Format(
                    CryptoResourceStrings.Crypt_Invalid_BlockSize,
                    value.Length * 8,
                    CryptoHelpers.FormatLegalSizes(legalBlockSizes)),
                paramValueName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfHashAlgorithmDestinationTooSmall(bool success)
    {
        if (!success)
            throw new CryptographicException(
                CryptoResourceStrings.Crypt_Invalid_HashAlgorithmDestinationBufferTooSmall);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ThrowIfHashAlgorithmProducedNoValue(byte[]? hash) =>
        hash ?? throw new CryptographicException(
            CryptoResourceStrings.Crypt_Invalid_HashAlgorithmDidNotProduceValue);

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
}

#endif
