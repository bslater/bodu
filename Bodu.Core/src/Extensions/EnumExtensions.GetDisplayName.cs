// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.GetDisplayName.cs" company="Bodu Pty. Ltd.">
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
    /// Returns the display name for the specified enumeration value.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The value to name.</param>
    /// <returns>The value's <see cref="DisplayAttribute" /> name, or its <see cref="DescriptionAttribute" /> text, falling back to the value's name.</returns>
    public static string GetDisplayName<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(this TEnum value)
        where TEnum : struct, Enum =>
        Enums.GetDisplayName(value);
}
