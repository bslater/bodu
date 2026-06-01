// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.ToggleFlag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class EnumExtensions
{
    /// <summary>
    /// Returns a copy of <paramref name="value" /> with every bit in <paramref name="flags" /> flipped.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The enumeration value to start from.</param>
    /// <param name="flags">The flag bits to toggle.</param>
    /// <returns>
    /// A new enumeration value equal to <paramref name="value" /> with <paramref name="flags" /> toggled.
    /// </returns>
    /// <remarks>
    /// Equivalent to the bitwise XOR <c>value ^ flags</c>: bits present in <paramref name="flags" /> are flipped, so
    /// applying the operation twice with the same <paramref name="flags" /> restores the original value.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TEnum ToggleFlag<TEnum>(this TEnum value, TEnum flags)
        where TEnum : struct, Enum =>
        FromUInt64<TEnum>(ToUInt64(value) ^ ToUInt64(flags));
}
