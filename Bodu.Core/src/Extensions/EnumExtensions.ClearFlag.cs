// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.ClearFlag.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class EnumExtensions
{
    /// <summary>
    /// Returns a copy of <paramref name="value" /> with every bit in <paramref name="flags" /> cleared.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The enumeration value to start from.</param>
    /// <param name="flags">The flag bits to clear.</param>
    /// <returns>
    /// A new enumeration value equal to <paramref name="value" /> with <paramref name="flags" /> removed.
    /// </returns>
    /// <remarks>
    /// Equivalent to <c>value &amp; ~flags</c>. Bits not present in <paramref name="flags" /> are left unchanged, and
    /// clearing a bit that was not set is a no-op.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TEnum ClearFlag<TEnum>(this TEnum value, TEnum flags)
        where TEnum : struct, Enum =>
        FromUInt64<TEnum>(ToUInt64(value) & ~ToUInt64(flags));
}
