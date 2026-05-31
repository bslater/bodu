// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.SetFlag.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class EnumExtensions
{
    /// <summary>
    /// Returns a copy of <paramref name="value" /> with every bit in <paramref name="flags" /> set.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The enumeration value to start from.</param>
    /// <param name="flags">The flag bits to set.</param>
    /// <returns>
    /// A new enumeration value equal to <paramref name="value" /> with <paramref name="flags" /> added.
    /// </returns>
    /// <remarks>
    /// Equivalent to the bitwise OR <c>value | flags</c>. The receiver is not modified; enumerations are value types
    /// and the result is returned as a new value.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TEnum SetFlag<TEnum>(this TEnum value, TEnum flags)
        where TEnum : struct, Enum =>
        FromUInt64<TEnum>(ToUInt64(value) | ToUInt64(flags));
}
