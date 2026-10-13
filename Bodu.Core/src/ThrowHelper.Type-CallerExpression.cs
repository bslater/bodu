// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Type-CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using System.Diagnostics.CodeAnalysis;
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
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a defined member of <typeparamref name="TEnum" />.
    /// </exception>
    /// <remarks>
    /// For an enumeration whose defined values form one contiguous range and that is not marked with
    /// <see cref="FlagsAttribute" />, the test is two comparisons against bounds cached once per type; any other
    /// enumeration is tested with <see cref="Enum.IsDefined{TEnum}(TEnum)" />. The answer is the same either way.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfEnumValueIsUndefined<TEnum>(
        TEnum value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TEnum : struct, Enum
    {
        if (!EnumValueRange<TEnum>.IsDefined(value))
            ThrowEnumValueIsUndefined(value, paramName);
    }

    /// <summary>
    /// Throws the <see cref="ArgumentOutOfRangeException" /> reported for a value an enumeration does not define.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The undefined value.</param>
    /// <param name="paramName">The name of the parameter that supplied <paramref name="value" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">Always thrown.</exception>
    [DoesNotReturn]
    private static void ThrowEnumValueIsUndefined<TEnum>(TEnum value, string? paramName)
        where TEnum : struct, Enum =>
        throw new ArgumentOutOfRangeException(
            paramName,
            string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_EnumValue, typeof(TEnum).Name, value));

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="comparison" /> is not a valid
    /// <see cref="StringComparison" /> value.
    /// </summary>
    /// <param name="comparison">The <see cref="StringComparison" /> value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="comparison" /> is not a defined enum member.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidStringComparison(
        StringComparison comparison,
        [CallerArgumentExpression(nameof(comparison))] string? paramName = null)
    {
        if (!Enum.IsDefined(comparison))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StringComparison, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="value" /> is not assignable to
    /// <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The target type to validate against.</typeparam>
    /// <param name="value">The object to check.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is not <see langword="null" /> and not of type <typeparamref name="T" />,
    /// or when <paramref name="value" /> is <see langword="null" /> and <typeparamref name="T" /> is a non-nullable
    /// value type.
    /// </exception>
    /// <remarks>
    /// A <see langword="null" /> value passes validation only when <typeparamref name="T" /> is a reference or nullable
    /// type.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotOfType<T>(
        object? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
        {
            if (default(T) is not null)
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_MustBeOfType, typeof(T)),
                    paramName);
        }
        else if (value is not T)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_MustBeOfType, typeof(T)),
                paramName);
        }
    }
}
