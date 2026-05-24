// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.BitConversion.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class EnumExtensions
{
    /// <summary>
    /// Reinterprets the bit pattern of <paramref name="value" /> as a 64-bit unsigned integer without boxing.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The enumeration value whose underlying bits are read.</param>
    /// <returns>The underlying integer value of <paramref name="value" />, zero-extended to 64 bits.</returns>
    /// <remarks>
    /// The enum is read at exactly its declared width (1, 2, 4, or 8 bytes); reading a wider type would observe
    /// adjacent storage. The width switch is resolved at JIT time for each closed <typeparamref name="TEnum" />, so the
    /// helper compiles down to a single load.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ToUInt64<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Unsafe.SizeOf<TEnum>() switch
        {
            1 => Unsafe.As<TEnum, byte>(ref value),
            2 => Unsafe.As<TEnum, ushort>(ref value),
            4 => Unsafe.As<TEnum, uint>(ref value),
            _ => Unsafe.As<TEnum, ulong>(ref value),
        };

    /// <summary>
    /// Reinterprets the low bits of <paramref name="value" /> as an enumeration of type <typeparamref name="TEnum" />.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The integer bit pattern to reinterpret.</param>
    /// <returns>An enumeration value whose underlying bits are the low bits of <paramref name="value" />.</returns>
    /// <remarks>
    /// Only the low bytes corresponding to the declared width of <typeparamref name="TEnum" /> are written; the
    /// remaining bits of <paramref name="value" /> are discarded, which is correct because every bitwise result is
    /// produced from operands of that same width.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TEnum FromUInt64<TEnum>(ulong value)
        where TEnum : struct, Enum
    {
        TEnum result = default;

        switch (Unsafe.SizeOf<TEnum>())
        {
            case 1:
                Unsafe.As<TEnum, byte>(ref result) = (byte)value;
                break;
            case 2:
                Unsafe.As<TEnum, ushort>(ref result) = (ushort)value;
                break;
            case 4:
                Unsafe.As<TEnum, uint>(ref result) = (uint)value;
                break;
            default:
                Unsafe.As<TEnum, ulong>(ref result) = value;
                break;
        }

        return result;
    }
}
