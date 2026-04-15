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
    /// Rotates the bits of the specified <see cref="byte"/> value to the left by the specified
    /// number of positions, without validating <paramref name="count"/>.
    /// </summary>
    /// <param name="value">The 8-bit unsigned value whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 8].</param>
    /// <returns>
    /// A <see cref="byte"/> with the bits of <paramref name="value"/> rotated left by <paramref name="count"/> positions.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as
    /// the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>, where
    /// <paramref name="count"/> is always a compile-time constant in the valid range. Prefer
    /// <see cref="RotateBitsLeft(byte, int)"/> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte RotateBitsLeftUnchecked(this byte value, int count) =>
        (byte)((value << count) | (value >> (8 - count)));

    /// <summary>
    /// Rotates the bits of the specified <see cref="ushort"/> value to the left by the specified
    /// number of positions, without validating <paramref name="count"/>.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 16].</param>
    /// <returns>
    /// A <see cref="ushort"/> with the bits of <paramref name="value"/> rotated left by <paramref name="count"/> positions.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as
    /// the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>, where
    /// <paramref name="count"/> is always a compile-time constant in the valid range. Prefer
    /// <see cref="RotateBitsLeft(ushort, int)"/> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort RotateBitsLeftUnchecked(this ushort value, int count) =>
        (ushort)((value << count) | (value >> (16 - count)));

    /// <summary>
    /// Rotates the bits of the specified <see cref="uint"/> value to the left by the specified
    /// number of positions, without validating <paramref name="count"/>.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 32].</param>
    /// <returns>
    /// A <see cref="uint"/> with the bits of <paramref name="value"/> rotated left by <paramref name="count"/> positions.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BitOperations.RotateLeft(uint, int)"/>, allowing the JIT to lower
    /// the rotation to a single CPU instruction. Intended for performance-critical, trusted callers
    /// such as the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>. Prefer
    /// <see cref="RotateBitsLeft(uint, int)"/> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RotateBitsLeftUnchecked(this uint value, int count) =>
        BitOperations.RotateLeft(value, count);

    /// <summary>
    /// Rotates the bits of the specified <see cref="ulong"/> value to the left by the specified
    /// number of positions, without validating <paramref name="count"/>.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 64].</param>
    /// <returns>
    /// A <see cref="ulong"/> with the bits of <paramref name="value"/> rotated left by <paramref name="count"/> positions.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BitOperations.RotateLeft(ulong, int)"/>, allowing the JIT to lower
    /// the rotation to a single CPU instruction. Intended for performance-critical, trusted callers
    /// such as the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>. Prefer
    /// <see cref="RotateBitsLeft(ulong, int)"/> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong RotateBitsLeftUnchecked(this ulong value, int count) =>
        BitOperations.RotateLeft(value, count);

    /// <summary>
    /// Rotates the bits of the specified <see cref="byte"/> value to the right by the specified
    /// number of positions, without validating <paramref name="count"/>.
    /// </summary>
    /// <param name="value">The 8-bit unsigned value whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 8].</param>
    /// <returns>
    /// A <see cref="byte"/> with the bits of <paramref name="value"/> rotated right by <paramref name="count"/> positions.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as
    /// the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>, where
    /// <paramref name="count"/> is always a compile-time constant in the valid range. Prefer
    /// <see cref="RotateBitsRight(byte, int)"/> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte RotateBitsRightUnchecked(this byte value, int count) =>
        (byte)((value >> count) | (value << (8 - count)));

    /// <summary>
    /// Rotates the bits of the specified <see cref="ushort"/> value to the right by the specified
    /// number of positions, without validating <paramref name="count"/>.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 16].</param>
    /// <returns>
    /// A <see cref="ushort"/> with the bits of <paramref name="value"/> rotated right by <paramref name="count"/> positions.
    /// </returns>
    /// <remarks>
    /// Provides an unguarded fast-path intended for performance-critical, trusted callers such as
    /// the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>, where
    /// <paramref name="count"/> is always a compile-time constant in the valid range. Prefer
    /// <see cref="RotateBitsRight(ushort, int)"/> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort RotateBitsRightUnchecked(this ushort value, int count) =>
        (ushort)((value >> count) | (value << (16 - count)));

    /// <summary>
    /// Rotates the bits of the specified <see cref="uint"/> value to the right by the specified
    /// number of positions, without validating <paramref name="count"/>.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 32].</param>
    /// <returns>
    /// A <see cref="uint"/> with the bits of <paramref name="value"/> rotated right by <paramref name="count"/> positions.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BitOperations.RotateRight(uint, int)"/>, allowing the JIT to lower
    /// the rotation to a single CPU instruction. Intended for performance-critical, trusted callers
    /// such as the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>. Prefer
    /// <see cref="RotateBitsRight(uint, int)"/> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint RotateBitsRightUnchecked(this uint value, int count) =>
        BitOperations.RotateRight(value, count);

    /// <summary>
    /// Rotates the bits of the specified <see cref="ulong"/> value to the right by the specified
    /// number of positions, without validating <paramref name="count"/>.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">The number of positions to rotate. The caller must guarantee a value in [0, 64].</param>
    /// <returns>
    /// A <see cref="ulong"/> with the bits of <paramref name="value"/> rotated right by <paramref name="count"/> positions.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BitOperations.RotateRight(ulong, int)"/>, allowing the JIT to lower
    /// the rotation to a single CPU instruction. Intended for performance-critical, trusted callers
    /// such as the hashing and cipher primitives in <c>Bodu.Security.Cryptography</c>. Prefer
    /// <see cref="RotateBitsRight(ulong, int)"/> in public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong RotateBitsRightUnchecked(this ulong value, int count) =>
        BitOperations.RotateRight(value, count);

    /// <summary>
    /// Reverses the byte order of the specified <see cref="ushort"/> value without performing any
    /// argument validation.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>
    /// A <see cref="ushort"/> whose bytes are in the reverse order of <paramref name="value"/>.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>. Intended for
    /// performance-critical, trusted callers such as the hashing and cipher primitives in
    /// <c>Bodu.Security.Cryptography</c>. Prefer <see cref="ReverseBytes(ushort)"/> in
    /// public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort ReverseBytesUnchecked(this ushort value) =>
        BinaryPrimitives.ReverseEndianness(value);

    /// <summary>
    /// Reverses the byte order of the specified <see cref="uint"/> value without performing any
    /// argument validation.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>
    /// A <see cref="uint"/> whose bytes are in the reverse order of <paramref name="value"/>.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BinaryPrimitives.ReverseEndianness(uint)"/>. Intended for
    /// performance-critical, trusted callers such as the hashing and cipher primitives in
    /// <c>Bodu.Security.Cryptography</c>. Prefer <see cref="ReverseBytes(uint)"/> in
    /// public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ReverseBytesUnchecked(this uint value) =>
        BinaryPrimitives.ReverseEndianness(value);

    /// <summary>
    /// Reverses the byte order of the specified <see cref="ulong"/> value without performing any
    /// argument validation.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>
    /// A <see cref="ulong"/> whose bytes are in the reverse order of <paramref name="value"/>.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BinaryPrimitives.ReverseEndianness(ulong)"/>. Intended for
    /// performance-critical, trusted callers such as the hashing and cipher primitives in
    /// <c>Bodu.Security.Cryptography</c>. Prefer <see cref="ReverseBytes(ulong)"/> in
    /// public-facing code.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ReverseBytesUnchecked(this ulong value) =>
        BinaryPrimitives.ReverseEndianness(value);
}
