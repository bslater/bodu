// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.HasAnyFlag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class EnumExtensions
{
    /// <summary>
    /// Determines whether <paramref name="value" /> has at least one of the bits set in <paramref name="flags" />.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The enumeration value to test.</param>
    /// <param name="flags">The flag bits to look for.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="value" /> and <paramref name="flags" /> share at least one set bit;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Equivalent to <c>(value &amp; flags) != 0</c> evaluated on the underlying integer type. When
    /// <paramref name="flags" /> has no bits set the result is always <see langword="false" />.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAnyFlag<TEnum>(this TEnum value, TEnum flags)
        where TEnum : struct, Enum =>
        (ToUInt64(value) & ToUInt64(flags)) != 0UL;
}
