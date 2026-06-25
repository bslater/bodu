// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Enums.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bodu.Extensions;

/// <summary>
/// Provides type-level helpers for enumerations: cached value and name lookups, parsing, and resolution of values from
/// their <see cref="DescriptionAttribute" /> or <see cref="DisplayAttribute" /> text.
/// </summary>
/// <remarks>
/// <para>
/// Value and name lookups are served from a per-enum cache populated once on first use and are backed by the
/// trimmer-friendly intrinsics <see cref="Enum.GetValues{TEnum}" /> and <see cref="Enum.GetNames{TEnum}" />.
/// Description and display-name lookups read the enum's field attributes by reflection; this is trim- and
/// ahead-of-time-safe because enum fields and the referenced attribute types are always preserved, so no
/// <see cref="RequiresUnreferencedCodeAttribute" /> is required.
/// </para>
/// </remarks>
public static class Enums
{
    /// <summary>
    /// Returns the values declared by the specified enumeration.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <returns>A fresh array of the values declared by <typeparamref name="TEnum" />.</returns>
    /// <remarks>
    /// A new array is returned on every call so callers may mutate the result without affecting the internal cache or
    /// other callers, matching the behavior of <see cref="Enum.GetValues{TEnum}" />. The underlying reflection result
    /// is cached, so repeated calls only pay the cost of a single array copy.
    /// </remarks>
    public static TEnum[] GetValues<TEnum>()
        where TEnum : struct, Enum =>
        (TEnum[])ValuesCache<TEnum>.Values.Clone();

    /// <summary>
    /// Returns the names declared by the specified enumeration.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <returns>A fresh array of the names declared by <typeparamref name="TEnum" />.</returns>
    /// <remarks>
    /// A new array is returned on every call so callers may mutate the result without affecting the internal cache or
    /// other callers, matching the behavior of <see cref="Enum.GetNames{TEnum}" />. The underlying reflection result is
    /// cached, so repeated calls only pay the cost of a single array copy.
    /// </remarks>
    public static string[] GetNames<TEnum>()
        where TEnum : struct, Enum =>
        (string[])ValuesCache<TEnum>.Names.Clone();

    /// <summary>
    /// Attempts to parse the specified text into a value of the enumeration.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">
    /// When this method returns, contains the parsed value, or the default value on failure.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse<TEnum>(string? text, out TEnum value)
        where TEnum : struct, Enum =>
        Enum.TryParse(text, out value);

    /// <summary>
    /// Attempts to parse the specified text into a value of the enumeration, optionally ignoring case.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <param name="ignoreCase"><see langword="true" /> to ignore case; otherwise, <see langword="false" />.</param>
    /// <param name="value">
    /// When this method returns, contains the parsed value, or the default value on failure.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse<TEnum>(string? text, bool ignoreCase, out TEnum value)
        where TEnum : struct, Enum =>
        Enum.TryParse(text, ignoreCase, out value);

    /// <summary>
    /// Attempts to resolve the enumeration value whose <see cref="DescriptionAttribute" /> or
    /// <see cref="DisplayAttribute" /> text matches the specified text.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="text">The description or display text to resolve.</param>
    /// <param name="value">
    /// When this method returns, contains the matching value, or the default value on failure.
    /// </param>
    /// <returns><see langword="true" /> if a matching value was found; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// Matching is ordinal. When more than one value declares the same description or display text, the first declaring
    /// value in ascending underlying-value order wins; the lookup is therefore deterministic.
    /// </remarks>
    public static bool TryParseDescription<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(string? text, out TEnum value)
        where TEnum : struct, Enum
    {
        if (text is not null && DescriptionCache<TEnum>.ByDescription.TryGetValue(text, out value))
            return true;

        value = default;
        return false;
    }

    /// <summary>
    /// Resolves the description text for the specified value, preferring its <see cref="DescriptionAttribute" />, then
    /// its <see cref="DisplayAttribute" /> name or short name, and falling back to the value's name.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The value to describe.</param>
    /// <returns>The resolved description text.</returns>
    internal static string GetDescription<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        DescriptionCache<TEnum>.Descriptions.TryGetValue(value, out string? description)
            ? description
            : value.ToString();

    /// <summary>
    /// Resolves the display name for the specified value, preferring its <see cref="DisplayAttribute" /> name, then its
    /// <see cref="DescriptionAttribute" /> text, and falling back to the value's name.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The value to name.</param>
    /// <returns>The resolved display name.</returns>
    internal static string GetDisplayName<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        DescriptionCache<TEnum>.DisplayNames.TryGetValue(value, out string? name)
            ? name
            : value.ToString();

    /// <summary>
    /// Attempts to resolve the explicit description text for the specified value.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The value to describe.</param>
    /// <param name="description">
    /// When this method returns, contains the explicit description if one is declared; otherwise, the value's name.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an explicit description or display attribute was found; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool TryGetDescription<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(TEnum value, out string description)
        where TEnum : struct, Enum
    {
        if (DescriptionCache<TEnum>.Descriptions.TryGetValue(value, out string? resolved))
        {
            description = resolved;
            return true;
        }

        description = value.ToString();
        return false;
    }

    /// <summary>
    /// Caches the values and names of an enumeration using trimmer-friendly intrinsics.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    private static class ValuesCache<TEnum>
        where TEnum : struct, Enum
    {
        /// <summary>The cached values of the enumeration.</summary>
        public static readonly TEnum[] Values = Enum.GetValues<TEnum>();

        /// <summary>The cached names of the enumeration.</summary>
        public static readonly string[] Names = Enum.GetNames<TEnum>();
    }

    /// <summary>
    /// Caches the attribute-derived description and display text of an enumeration.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    private static class DescriptionCache<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>
        where TEnum : struct, Enum
    {
        /// <summary>Maps each value with an explicit attribute to its resolved description text.</summary>
        public static readonly Dictionary<TEnum, string> Descriptions = new();

        /// <summary>Maps each value with an explicit attribute to its resolved display name.</summary>
        public static readonly Dictionary<TEnum, string> DisplayNames = new();

        /// <summary>Maps each resolved description back to its first declaring value.</summary>
        public static readonly Dictionary<string, TEnum> ByDescription = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes static members of the <see cref="DescriptionCache{TEnum}" /> class class by reflecting over the
        /// enumeration's fields.
        /// </summary>
        static DescriptionCache()
        {
            Type type = typeof(TEnum);
            foreach (TEnum value in Enum.GetValues<TEnum>())
            {
                string name = value.ToString();
                FieldInfo? field = type.GetField(name);
                if (field is null)
                    continue;

                string? description = field.GetCustomAttribute<DescriptionAttribute>()?.Description;
                DisplayAttribute? display = field.GetCustomAttribute<DisplayAttribute>();
                string? displayName = display?.GetName();
                string? displayShort = display?.GetShortName();

                string? resolvedDescription = description ?? displayName ?? displayShort;
                if (resolvedDescription is not null)
                {
                    Descriptions[value] = resolvedDescription;
                    if (!ByDescription.ContainsKey(resolvedDescription))
                        ByDescription[resolvedDescription] = value;
                }

                string? resolvedDisplay = displayName ?? description;
                if (resolvedDisplay is not null)
                    DisplayNames[value] = resolvedDisplay;
            }
        }
    }
}
