// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.GetDescription.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Bodu.Extensions;

public static partial class EnumExtensions
{
    /// <summary>
    /// Returns the description text for the specified enumeration value.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The value to describe.</param>
    /// <returns>The value's <see cref="DescriptionAttribute" /> text, or its <see cref="DisplayAttribute" /> name or short name, falling back to the value's name.</returns>
    public static string GetDescription<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(this TEnum value)
        where TEnum : struct, Enum =>
        Enums.GetDescription(value);

    /// <summary>
    /// Attempts to return the explicit description text declared for the specified enumeration value.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The value to describe.</param>
    /// <param name="description">When this method returns, contains the explicit description if one is declared; otherwise, the value's name.</param>
    /// <returns><see langword="true" /> if an explicit description or display attribute was declared; otherwise, <see langword="false" />.</returns>
    public static bool TryGetDescription<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(this TEnum value, out string description)
        where TEnum : struct, Enum =>
        Enums.TryGetDescription(value, out description);
}
