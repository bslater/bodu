// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Type.NetStandard.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if NETSTANDARD2_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not a defined member of
    /// <typeparamref name="TEnum" />.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a defined member of <typeparamref name="TEnum" />.
    /// </exception>
    public static void ThrowIfEnumValueIsUndefined<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
            throw new ArgumentOutOfRangeException(
                nameof(value),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_EnumValue, typeof(TEnum).Name, value));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="comparison" /> is not a valid
    /// <see cref="StringComparison" /> value.
    /// </summary>
    /// <param name="comparison">The <see cref="StringComparison" /> value to validate.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="comparison" /> is not a defined enum member.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidStringComparison(StringComparison comparison)
    {
        if (!Enum.IsDefined(typeof(StringComparison), comparison))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StringComparison, nameof(comparison));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="value" /> is not assignable to
    /// <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type to validate against.</typeparam>
    /// <param name="value">The object to check.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is not <see langword="null" /> and not of type
    /// <typeparamref name="T" />, or when <paramref name="value" /> is <see langword="null" /> and
    /// <typeparamref name="T" /> is a non-nullable value type.
    /// </exception>
    /// <remarks>
    /// A <see langword="null" /> value passes validation only when <typeparamref name="T" /> is a reference or
    /// nullable type.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotOfType<T>(object? value)
    {
        if (value is null)
        {
            if (default(T) is not null)
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_MustBeOfType, typeof(T)),
                    nameof(value));
        }
        else if (value is not T)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_MustBeOfType, typeof(T)),
                nameof(value));
        }
    }
}

#endif
