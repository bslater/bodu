// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.Unchecked.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{

    /// <summary>
    /// Reverses the order of the least significant <paramref name="bitLength" /> bits in the specified
    /// <see cref="byte" /> value, without validating <paramref name="bitLength" />.
    /// </summary>
    /// <param name="value">The 8-bit unsigned value to reflect.</param>
    /// <param name="bitLength">
    /// The number of least significant bits to reverse. The caller must guarantee a value in [0, 8].
    /// </param>
    /// <returns>
    /// A <see cref="byte" /> whose least significant <paramref name="bitLength" /> bits contain the bit-reversed window
    /// of <paramref name="value" />, with all higher bits set to zero.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as the hashing and
    /// checksum primitives in <c>Bodu.Security.Cryptography</c>, where <paramref name="bitLength" /> is a compile-time
    /// constant or already validated. Prefer <see cref="ReverseBits(byte, int)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte ReverseBitsUnchecked(this byte value, int bitLength)
    {
        byte result = 0;

        for (var i = 0; i < bitLength; i++)
        {
            result = (byte)((result << 1) | (value & 1));
            value >>= 1;
        }

        return result;
    }

    /// <summary>
    /// Reverses the order of the least significant <paramref name="bitLength" /> bits in the specified
    /// <see cref="ushort" /> value, without validating <paramref name="bitLength" />.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer to reflect.</param>
    /// <param name="bitLength">
    /// The number of least significant bits to reverse. The caller must guarantee a value in [0, 16].
    /// </param>
    /// <returns>
    /// A <see cref="ushort" /> whose least significant <paramref name="bitLength" /> bits contain the bit-reversed
    /// window of <paramref name="value" />, with all higher bits set to zero.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as the hashing and
    /// checksum primitives in <c>Bodu.Security.Cryptography</c>, where <paramref name="bitLength" /> is a compile-time
    /// constant or already validated. Prefer <see cref="ReverseBits(ushort, int)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort ReverseBitsUnchecked(this ushort value, int bitLength)
    {
        ushort result = 0;

        for (var i = 0; i < bitLength; i++)
        {
            result = (ushort)((result << 1) | (value & 1));
            value >>= 1;
        }

        return result;
    }

    /// <summary>
    /// Reverses the order of the least significant <paramref name="bitLength" /> bits in the specified
    /// <see cref="uint" /> value, without validating <paramref name="bitLength" />.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer to reflect.</param>
    /// <param name="bitLength">
    /// The number of least significant bits to reverse. The caller must guarantee a value in [0, 32].
    /// </param>
    /// <returns>
    /// A <see cref="uint" /> whose least significant <paramref name="bitLength" /> bits contain the bit-reversed window
    /// of <paramref name="value" />, with all higher bits set to zero.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as the hashing and
    /// checksum primitives in <c>Bodu.Security.Cryptography</c>, where <paramref name="bitLength" /> is a compile-time
    /// constant or already validated. Prefer <see cref="ReverseBits(uint, int)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ReverseBitsUnchecked(this uint value, int bitLength)
    {
        uint result = 0;

        for (var i = 0; i < bitLength; i++)
        {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }

        return result;
    }

    /// <summary>
    /// Reverses the order of the least significant <paramref name="bitLength" /> bits in the specified
    /// <see cref="ulong" /> value, without validating <paramref name="bitLength" />.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer to reflect.</param>
    /// <param name="bitLength">
    /// The number of least significant bits to reverse. The caller must guarantee a value in [0, 64].
    /// </param>
    /// <returns>
    /// A <see cref="ulong" /> whose least significant <paramref name="bitLength" /> bits contain the bit-reversed
    /// window of <paramref name="value" />, with all higher bits set to zero.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as the hashing and
    /// checksum primitives in <c>Bodu.Security.Cryptography</c>, where <paramref name="bitLength" /> is a compile-time
    /// constant or already validated. Prefer <see cref="ReverseBits(ulong, int)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ReverseBitsUnchecked(this ulong value, int bitLength)
    {
        ulong result = 0;

        for (var i = 0; i < bitLength; i++)
        {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }

        return result;
    }

    /// <summary>
    /// Reverses the byte order of the specified <see cref="ushort" /> value without performing any argument validation.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>A <see cref="ushort" /> whose bytes are in the reverse order of <paramref name="value" />.</returns>
    /// <remarks>
    /// Delegates to <see cref="BinaryPrimitives.ReverseEndianness(ushort)" />. Intended for performance-critical,
    /// trusted callers such as the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>. Prefer
    /// <see cref="ReverseBytes(ushort)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort ReverseBytesUnchecked(this ushort value) =>
        BinaryPrimitives.ReverseEndianness(value);

    /// <summary>
    /// Reverses the byte order of the specified <see cref="uint" /> value without performing any argument validation.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>A <see cref="uint" /> whose bytes are in the reverse order of <paramref name="value" />.</returns>
    /// <remarks>
    /// Delegates to <see cref="BinaryPrimitives.ReverseEndianness(uint)" />. Intended for performance-critical, trusted
    /// callers such as the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>. Prefer
    /// <see cref="ReverseBytes(uint)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ReverseBytesUnchecked(this uint value) =>
        BinaryPrimitives.ReverseEndianness(value);

    /// <summary>
    /// Reverses the byte order of the specified <see cref="ulong" /> value without performing any argument validation.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>A <see cref="ulong" /> whose bytes are in the reverse order of <paramref name="value" />.</returns>
    /// <remarks>
    /// Delegates to <see cref="BinaryPrimitives.ReverseEndianness(ulong)" />. Intended for performance-critical,
    /// trusted callers such as the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>. Prefer
    /// <see cref="ReverseBytes(ulong)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ReverseBytesUnchecked(this ulong value) =>
        BinaryPrimitives.ReverseEndianness(value);
    /// <summary>
    /// Rotates the bits of the specified <see cref="byte" /> value to the left by the specified number of positions,
    /// without validating <paramref name="count" />.
    /// </summary>
    /// <param name="value">The 8-bit unsigned value whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 8].</param>
    /// <returns>
    /// A <see cref="byte" /> with the bits of <paramref name="value" /> rotated left by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as the hashing and
    /// cipher primitives in <c>Bodu.Security.Cryptography</c>, where <paramref name="count" /> is always a compile-time
    /// constant in the valid range. Prefer <see cref="RotateBitsLeft(byte, int)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte RotateBitsLeftUnchecked(this byte value, int count) =>
        (byte)((value << count) | (value >> (8 - count)));

    /// <summary>
    /// Rotates the bits of the specified <see cref="ushort" /> value to the left by the specified number of positions,
    /// without validating <paramref name="count" />.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 16].</param>
    /// <returns>
    /// A <see cref="ushort" /> with the bits of <paramref name="value" /> rotated left by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as the hashing and
    /// cipher primitives in <c>Bodu.Security.Cryptography</c>, where <paramref name="count" /> is always a compile-time
    /// constant in the valid range. Prefer <see cref="RotateBitsLeft(ushort, int)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort RotateBitsLeftUnchecked(this ushort value, int count) =>
        (ushort)((value << count) | (value >> (16 - count)));

    /// <summary>
    /// Rotates the bits of the specified <see cref="uint" /> value to the left by the specified number of positions,
    /// without validating <paramref name="count" />.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 32].</param>
    /// <returns>
    /// A <see cref="uint" /> with the bits of <paramref name="value" /> rotated left by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BitOperations.RotateLeft(uint, int)" />, allowing the JIT to lower the rotation to a
    /// single CPU instruction. Intended for performance-critical, trusted callers such as the hashing and cipher
    /// primitives in <c>Bodu.Security.Cryptography</c>. Prefer <see cref="RotateBitsLeft(uint, int)" /> in
    /// public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RotateBitsLeftUnchecked(this uint value, int count) =>
        BitOperations.RotateLeft(value, count);

    /// <summary>
    /// Rotates the bits of the specified <see cref="ulong" /> value to the left by the specified number of positions,
    /// without validating <paramref name="count" />.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 64].</param>
    /// <returns>
    /// A <see cref="ulong" /> with the bits of <paramref name="value" /> rotated left by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BitOperations.RotateLeft(ulong, int)" />, allowing the JIT to lower the rotation to a
    /// single CPU instruction. Intended for performance-critical, trusted callers such as the hashing and cipher
    /// primitives in <c>Bodu.Security.Cryptography</c>. Prefer <see cref="RotateBitsLeft(ulong, int)" /> in
    /// public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong RotateBitsLeftUnchecked(this ulong value, int count) =>
        BitOperations.RotateLeft(value, count);

    /// <summary>
    /// Rotates the bits of the specified <see cref="byte" /> value to the right by the specified number of positions,
    /// without validating <paramref name="count" />.
    /// </summary>
    /// <param name="value">The 8-bit unsigned value whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 8].</param>
    /// <returns>
    /// A <see cref="byte" /> with the bits of <paramref name="value" /> rotated right by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as the hashing and
    /// cipher primitives in <c>Bodu.Security.Cryptography</c>, where <paramref name="count" /> is always a compile-time
    /// constant in the valid range. Prefer <see cref="RotateBitsRight(byte, int)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte RotateBitsRightUnchecked(this byte value, int count) =>
        (byte)((value >> count) | (value << (8 - count)));

    /// <summary>
    /// Rotates the bits of the specified <see cref="ushort" /> value to the right by the specified number of positions,
    /// without validating <paramref name="count" />.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 16].</param>
    /// <returns>
    /// A <see cref="ushort" /> with the bits of <paramref name="value" /> rotated right by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as the hashing and
    /// cipher primitives in <c>Bodu.Security.Cryptography</c>, where <paramref name="count" /> is always a compile-time
    /// constant in the valid range. Prefer <see cref="RotateBitsRight(ushort, int)" /> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort RotateBitsRightUnchecked(this ushort value, int count) =>
        (ushort)((value >> count) | (value << (16 - count)));

    /// <summary>
    /// Rotates the bits of the specified <see cref="uint" /> value to the right by the specified number of positions,
    /// without validating <paramref name="count" />.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 32].</param>
    /// <returns>
    /// A <see cref="uint" /> with the bits of <paramref name="value" /> rotated right by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BitOperations.RotateRight(uint, int)" />, allowing the JIT to lower the rotation to a
    /// single CPU instruction. Intended for performance-critical, trusted callers such as the hashing and cipher
    /// primitives in <c>Bodu.Security.Cryptography</c>. Prefer <see cref="RotateBitsRight(uint, int)" /> in
    /// public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RotateBitsRightUnchecked(this uint value, int count) =>
        BitOperations.RotateRight(value, count);

    /// <summary>
    /// Rotates the bits of the specified <see cref="ulong" /> value to the right by the specified number of positions,
    /// without validating <paramref name="count" />.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 64].</param>
    /// <returns>
    /// A <see cref="ulong" /> with the bits of <paramref name="value" /> rotated right by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BitOperations.RotateRight(ulong, int)" />, allowing the JIT to lower the rotation to a
    /// single CPU instruction. Intended for performance-critical, trusted callers such as the hashing and cipher
    /// primitives in <c>Bodu.Security.Cryptography</c>. Prefer <see cref="RotateBitsRight(ulong, int)" /> in
    /// public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong RotateBitsRightUnchecked(this ulong value, int count) =>
        BitOperations.RotateRight(value, count);

}
