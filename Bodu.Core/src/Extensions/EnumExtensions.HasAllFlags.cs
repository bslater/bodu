// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.HasAllFlags.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class EnumExtensions
{
    /// <summary>
    /// Determines whether <paramref name="value" /> has every bit set that is set in <paramref name="flags" />.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The enumeration value to test.</param>
    /// <param name="flags">The flag bits that must all be present.</param>
    /// <returns>
    /// <see langword="true" /> if every bit set in <paramref name="flags" /> is also set in <paramref name="value" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Equivalent to <c>(value &amp; flags) == flags</c> evaluated on the underlying integer type. When
    /// <paramref name="flags" /> has no bits set the result is always <see langword="true" />. This is the non-boxing
    /// counterpart of <see cref="System.Enum.HasFlag(System.Enum)" />.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAllFlags<TEnum>(this TEnum value, TEnum flags)
        where TEnum : struct, Enum
    {
        ulong mask = ToUInt64(flags);
        return (ToUInt64(value) & mask) == mask;
    }
}
