// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumValueRange{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu;

/// <summary>
/// Caches, for one enumeration type, whether its defined values form a single contiguous range, so that testing a value
/// for definition costs two comparisons rather than <see cref="Enum.IsDefined{TEnum}(TEnum)" />'s metadata lookup.
/// </summary>
/// <typeparam name="TEnum">The enumeration type.</typeparam>
/// <remarks>
/// <para>
/// An enumeration marked with <see cref="FlagsAttribute" />, one with gaps between its values, and one with no values
/// keep <see cref="Enum.IsDefined{TEnum}(TEnum)" />, so the answer is always the one it gives.
/// </para>
/// <para>
/// Values are compared as 64-bit integers: a signed underlying type is widened with its sign, an unsigned one without,
/// and an unsigned 64-bit value is reinterpreted. The cached bounds are read through the same conversion as the value
/// tested, so the range test is exact whatever the underlying type.
/// </para>
/// </remarks>
internal static class EnumValueRange<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>Indicates whether the enumeration's underlying type is signed.</summary>
    private static readonly bool s_isSigned = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(TEnum))) is TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64;

    /// <summary>The enumeration's distinct defined values, widened and in ascending order, or an empty array when the range test does not apply.</summary>
    private static readonly long[] s_values = ReadValues();

    /// <summary>Indicates whether the defined values form one contiguous range, from <see cref="s_min" /> to <see cref="s_max" />.</summary>
    private static readonly bool s_isContiguous = s_values.Length > 0 && (ulong)(s_values[^1] - s_values[0]) == (ulong)(s_values.Length - 1);

    /// <summary>The smallest defined value, widened, when the values are contiguous.</summary>
    private static readonly long s_min = s_isContiguous ? s_values[0] : 0;

    /// <summary>The largest defined value, widened, when the values are contiguous.</summary>
    private static readonly long s_max = s_isContiguous ? s_values[^1] : 0;

    /// <summary>
    /// Determines whether a value is one the enumeration defines.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> equals a defined value; otherwise
    /// <see langword="false" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsDefined(TEnum value)
    {
        if (!s_isContiguous)
            return Enum.IsDefined(value);

        long widened = Widen(value);
        return widened >= s_min && widened <= s_max;
    }

    /// <summary>
    /// Widens a value of the enumeration to a 64-bit integer, keeping the sign of a signed underlying type.
    /// </summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The widened value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Widen(TEnum value)
    {
        if (Unsafe.SizeOf<TEnum>() == sizeof(byte))
            return s_isSigned ? Unsafe.As<TEnum, sbyte>(ref value) : Unsafe.As<TEnum, byte>(ref value);

        if (Unsafe.SizeOf<TEnum>() == sizeof(short))
            return s_isSigned ? Unsafe.As<TEnum, short>(ref value) : Unsafe.As<TEnum, ushort>(ref value);

        if (Unsafe.SizeOf<TEnum>() == sizeof(int))
            return s_isSigned ? Unsafe.As<TEnum, int>(ref value) : Unsafe.As<TEnum, uint>(ref value);

        return Unsafe.As<TEnum, long>(ref value);
    }

    /// <summary>
    /// Reads the enumeration's distinct defined values, widened and sorted, unless it is a flags enumeration.
    /// </summary>
    /// <returns>The values, or an empty array for a flags enumeration.</returns>
    private static long[] ReadValues()
    {
        if (typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false))
            return [];

        TEnum[] defined = Enum.GetValues<TEnum>();
        var values = new long[defined.Length];
        for (int i = 0; i < defined.Length; i++)
            values[i] = Widen(defined[i]);

        Array.Sort(values);

        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (count == 0 || values[i] != values[count - 1])
                values[count++] = values[i];
        }

        return values[..count];
    }
}
