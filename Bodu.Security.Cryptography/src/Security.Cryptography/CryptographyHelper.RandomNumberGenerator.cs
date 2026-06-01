// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptographyHelper.RandomNumberGenerator.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal static partial class CryptographyHelper
{
    /// <summary>
    /// Fills the specified byte array with cryptographically secure random bytes, ensuring that no byte is equal to
    /// <c>0x00</c>.
    /// </summary>
    /// <param name="buffer">The byte array to fill.</param>
    /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="buffer" /> is empty.</exception>
    /// <remarks>
    /// Delegates to the span-based overload. Random generation is repeated until the buffer contains no zero bytes.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void FillWithRandomNonZeroBytes(byte[] buffer)
    {
        ThrowHelper.ThrowIfNull(buffer);
        ThrowHelper.ThrowIfArrayLengthIsZero(buffer);
        FillWithRandomNonZeroBytes(buffer.AsSpan());
    }

    /// <summary>
    /// Fills the provided span with cryptographically secure random bytes, ensuring that no byte in the span is equal
    /// to <c>0x00</c>.
    /// </summary>
    /// <param name="buffer">The span to fill.</param>
    /// <remarks>
    /// Loops until all bytes are non-zero. Uses <see cref="RandomNumberGenerator.Fill(Span{byte})" />.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void FillWithRandomNonZeroBytes(Span<byte> buffer) => FillWithRandomBytesExcluding(0x00, buffer);

    /// <summary>
    /// Attempts to fill the provided span with cryptographically secure random bytes that do not include <c>0x00</c>,
    /// redrawing each individual zero byte up to a bounded number of times.
    /// </summary>
    /// <param name="buffer">The span to fill.</param>
    /// <returns>
    /// <see langword="true" /> if the buffer was filled successfully without any zero bytes; otherwise,
    /// <see langword="false" /> once the per-byte redraw limit has been reached for any position.
    /// </returns>
    /// <remarks>
    /// Uses per-byte rejection sampling: the buffer is filled once, and only positions that came up zero are
    /// individually redrawn. This scales linearly with buffer length rather than exponentially, so the failure
    /// probability stays negligible (≈ 256<sup>-8</sup> per byte) regardless of how large the buffer is. Intended for
    /// performance-sensitive scenarios where indefinite retry is undesirable.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryFillWithRandomNonZeroBytes(Span<byte> buffer)
    {
        const int maxRedrawsPerByte = 8;

#if NETSTANDARD2_0
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] temp = new byte[buffer.Length];
            try
            {
                rng.GetBytes(temp);

                // Per-byte rejection: only redraw the positions that came up zero.
                byte[] single = new byte[1];
                for (int i = 0; i < temp.Length; i++)
                {
                    int redraws = 0;
                    while (temp[i] == 0)
                    {
                        if (redraws == maxRedrawsPerByte)
                            return false;

                        rng.GetBytes(single);
                        temp[i] = single[0];
                        redraws++;
                    }
                }

                single[0] = 0;
                temp.CopyTo(buffer);
                return true;
            }
            finally
            {
                // Wipe temp so any random bytes drawn (success or failure path) do not linger on the managed heap.
                Array.Clear(temp, 0, temp.Length);
            }
        }
#else
        RandomNumberGenerator.Fill(buffer);

        // Per-byte rejection: only redraw the positions that came up zero.
        Span<byte> single = stackalloc byte[1];
        for (var i = 0; i < buffer.Length; i++)
        {
            var redraws = 0;
            while (buffer[i] == 0)
            {
                if (redraws == maxRedrawsPerByte)
                    return false;

                RandomNumberGenerator.Fill(single);
                buffer[i] = single[0];
                redraws++;
            }
        }

        return true;
#endif
    }

    /// <summary>
    /// Returns a new byte array filled with cryptographically secure random bytes, none of which are zero.
    /// </summary>
    /// <param name="length">The number of random bytes to generate. Must be greater than zero.</param>
    /// <returns>A <see cref="byte" /> array of the specified length containing only non-zero values.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="length" /> is less than or equal to zero.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte[] GetRandomNonZeroBytes(int length)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(length, 0);
        var buffer = GC.AllocateUninitializedArray<byte>(length);
        FillWithRandomNonZeroBytes(buffer.AsSpan());
        return buffer;
    }

    /// <summary>
    /// Fills the provided span with cryptographically secure random bytes, drawn uniformly over the full <c>0x00</c>–
    /// <c>0xFF</c> range.
    /// </summary>
    /// <param name="buffer">The span to fill.</param>
    /// <remarks>
    /// Unlike <see cref="FillWithRandomNonZeroBytes(Span{byte})" />, no byte value is excluded, so the result is an
    /// unbiased uniform sample. Use this for key and nonce generation, where every byte value is a legal input.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void FillWithRandomBytes(Span<byte> buffer) =>
#if NETSTANDARD2_0
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] temp = new byte[buffer.Length];
            try
            {
                rng.GetBytes(temp);
                temp.CopyTo(buffer);
            }
            finally
            {
                Array.Clear(temp, 0, temp.Length);
            }
        }
#else
        RandomNumberGenerator.Fill(buffer);
#endif


    /// <summary>
    /// Returns a new byte array filled with cryptographically secure random bytes, drawn uniformly over the full
    /// <c>0x00</c>–<c>0xFF</c> range.
    /// </summary>
    /// <param name="length">The number of random bytes to generate. Must be greater than zero.</param>
    /// <returns>A <see cref="byte" /> array of the specified length containing uniformly random bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="length" /> is less than or equal to zero.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte[] GetRandomBytes(int length)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(length, 0);
        var buffer = GC.AllocateUninitializedArray<byte>(length);
        FillWithRandomBytes(buffer.AsSpan());
        return buffer;
    }

    /// <summary>
    /// Fills the specified span with random bytes, excluding the given <paramref name="forbidden" /> byte.
    /// </summary>
    /// <param name="forbidden">The byte value to exclude from the result.</param>
    /// <param name="buffer">The span to fill with random bytes.</param>
    /// <remarks>
    /// Repeatedly fills the buffer until <paramref name="forbidden" /> is no longer present.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void FillWithRandomBytesExcluding(byte forbidden, Span<byte> buffer)
    {
#if NETSTANDARD2_0
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] temp = new byte[buffer.Length];
            try
            {
                rng.GetBytes(temp);

                // Targeted per-byte replacement: only re-draw for the bytes that
                // matched the forbidden value, rather than re-filling the whole buffer.
                byte[] single = new byte[1];
                for (int i = 0; i < temp.Length; i++)
                {
                    while (temp[i] == forbidden)
                    {
                        rng.GetBytes(single);
                        temp[i] = single[0];
                    }
                }

                single[0] = 0;
                temp.CopyTo(buffer);
            }
            finally
            {
                Array.Clear(temp, 0, temp.Length);
            }
        }
#else
        RandomNumberGenerator.Fill(buffer);

        // Targeted per-byte replacement: only re-draw for the bytes that
        // matched the forbidden value, rather than re-filling the whole buffer.
        Span<byte> single = stackalloc byte[1];
        for (var i = 0; i < buffer.Length; i++)
        {
            while (buffer[i] == forbidden)
            {
                RandomNumberGenerator.Fill(single);
                buffer[i] = single[0];
            }
        }
#endif
    }

    /// <summary>
    /// Returns a new array of cryptographically secure random bytes of the specified <paramref name="length" />,
    /// excluding any occurrences of the <paramref name="forbidden" /> byte value.
    /// </summary>
    /// <param name="forbidden">The byte value to exclude from the output.</param>
    /// <param name="length">The number of bytes to generate. Must be greater than zero.</param>
    /// <returns>
    /// A <see cref="byte" /> array filled with random bytes that do not include <paramref name="forbidden" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="length" /> is less than or equal to zero.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte[] GetRandomBytesExcluding(byte forbidden, int length)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(length, 0);
        var buffer = GC.AllocateUninitializedArray<byte>(length);
        FillWithRandomBytesExcluding(forbidden, buffer.AsSpan());
        return buffer;
    }
}
