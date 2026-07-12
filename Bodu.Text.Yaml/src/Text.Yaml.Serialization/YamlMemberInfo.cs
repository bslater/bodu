// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlMemberInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Describes a serializable property or field discovered by reflection, with cached accessors and the metadata needed
/// to compute its YAML key.
/// </summary>
internal sealed class YamlMemberInfo
{
    /// <summary>Caches the discovered member set per type so reflection runs once per type.</summary>
    private static readonly ConcurrentDictionary<Type, YamlMemberInfo[]> s_cache = new();

    /// <summary>
    /// Gets the declared member name.
    /// </summary>
    /// <value>The CLR member name.</value>
    public required string MemberName { get; init; }

    /// <summary>
    /// Gets the explicit YAML key supplied by an attribute, if any.
    /// </summary>
    /// <value>The explicit key, or <see langword="null" />.</value>
    public string? ExplicitName { get; init; }

    /// <summary>
    /// Gets the member's value type.
    /// </summary>
    /// <value>The member type.</value>
    public required Type Type { get; init; }

    /// <summary>
    /// Gets the accessor that reads the member's value from an instance.
    /// </summary>
    /// <value>The getter delegate.</value>
    public required Func<object, object?> Get { get; init; }

    /// <summary>
    /// Gets the accessor that writes the member's value to an instance, when writable.
    /// </summary>
    /// <value>The setter delegate, or <see langword="null" /> when the member is read-only.</value>
    public Action<object, object?>? Set { get; init; }

    /// <summary>
    /// Gets a value indicating whether the member must be present in the input.
    /// </summary>
    /// <value><see langword="true" /> when the member is required; otherwise <see langword="false" />.</value>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets the relative write order of the member; members are emitted in ascending order.
    /// </summary>
    /// <value>The order value, or zero when the member declares none.</value>
    public int Order { get; init; }

    /// <summary>
    /// Gets the condition under which the member is omitted on write, or <see langword="null" /> when it declares no
    /// per-member condition (in which case the serializer-wide null handling applies).
    /// </summary>
    /// <value>The conditional-ignore setting, or <see langword="null" />.</value>
    public IgnoreCondition? Condition { get; init; }

    /// <summary>
    /// Gets a value indicating whether the member is opted into serialization through <see cref="IncludeAttribute" />,
    /// which binds a non-public property setter and surfaces a public field even when fields are otherwise excluded.
    /// </summary>
    /// <value><see langword="true" /> when the member is force-included; otherwise <see langword="false" />.</value>
    public bool Include { get; init; }

    /// <summary>
    /// Gets a value indicating whether the member is the type's extension-data member, which captures unmapped keys
    /// rather than participating as a normal mapped member.
    /// </summary>
    /// <value><see langword="true" /> for the extension-data member; otherwise <see langword="false" />.</value>
    public bool IsExtensionData { get; init; }

    /// <summary>
    /// Computes the YAML key for this member under the given options.
    /// </summary>
    /// <param name="options">The serializer options.</param>
    /// <returns>The YAML key.</returns>
    public string WireName(YamlSerializerOptions options) =>
        ExplicitName ?? options.PropertyNamingPolicy?.ConvertName(MemberName) ?? MemberName;

    /// <summary>
    /// Verifies that no two members resolve to the same YAML key under the given options.
    /// </summary>
    /// <param name="members">The members to validate.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="type">The owning type, used in the failure message.</param>
    /// <exception cref="InvalidOperationException">Two members map to the same YAML key.</exception>
    public static void EnsureUniqueWireNames(YamlMemberInfo[] members, YamlSerializerOptions options, Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (YamlMemberInfo member in members)
        {
            if (member.IsExtensionData)
                continue;

            if (!seen.Add(member.WireName(options)))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_DuplicateWireName, type, member.WireName(options)));
            }
        }
    }

    /// <summary>
    /// Gets the cached, serializable members of a type, discovering them on first use.
    /// </summary>
    /// <param name="type">The type to reflect over.</param>
    /// <param name="includeFields">Whether public fields are included.</param>
    /// <returns>The serializable members.</returns>
    public static YamlMemberInfo[] ForType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        bool includeFields)
    {
        YamlMemberInfo[] all = s_cache.GetOrAdd(type, static t => Discover(t));
        if (includeFields)
            return all;

        // A public field is surfaced only when fields are enabled or the field opts in with [Include].
        return Array.FindAll(all, static m => !m.IsField || m.Include);
    }

    /// <summary>
    /// Gets a value indicating whether the member is a field rather than a property.
    /// </summary>
    /// <value><see langword="true" /> for a field; otherwise <see langword="false" />.</value>
    public bool IsField { get; init; }

    /// <summary>
    /// Discovers the serializable properties and fields of a type via reflection.
    /// </summary>
    /// <param name="type">The type to reflect over.</param>
    /// <returns>The discovered members.</returns>
    private static YamlMemberInfo[] Discover(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
    {
        var members = new List<YamlMemberInfo>();

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetMethod is null)
                continue;

            IgnoreAttribute? ignore = property.GetCustomAttribute<IgnoreAttribute>(inherit: true);
            if (ignore is { Condition: IgnoreCondition.Always })
                continue;

            bool include = property.IsDefined(typeof(IncludeAttribute), inherit: true);
            bool isExtension = property.IsDefined(typeof(ExtensionDataAttribute), inherit: true);
            if (isExtension && !IsExtensionDataCompatible(property.PropertyType))
            {
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlExtensionDataType, property.Name, type));
            }

            string? explicitName = property.GetCustomAttribute<PropertyNameAttribute>(inherit: true)?.Name;
            Action<object, object?>? setter = null;
            if (property.SetMethod is not null && (property.SetMethod.IsPublic || include))
                setter = property.SetValue;

            members.Add(new YamlMemberInfo
            {
                MemberName = property.Name,
                ExplicitName = explicitName,
                Type = property.PropertyType,
                Get = property.GetValue,
                Set = setter,
                IsRequired = IsRequiredMember(property),
                Order = property.GetCustomAttribute<PropertyOrderAttribute>(inherit: true)?.Order ?? 0,
                Condition = ignore?.Condition,
                Include = include,
                IsExtensionData = isExtension,
                IsField = false,
            });
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            IgnoreAttribute? ignore = field.GetCustomAttribute<IgnoreAttribute>(inherit: true);
            if (ignore is { Condition: IgnoreCondition.Always })
                continue;

            bool isExtension = field.IsDefined(typeof(ExtensionDataAttribute), inherit: true);
            if (isExtension && !IsExtensionDataCompatible(field.FieldType))
            {
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlExtensionDataType, field.Name, type));
            }

            string? explicitName = field.GetCustomAttribute<PropertyNameAttribute>(inherit: true)?.Name;
            members.Add(new YamlMemberInfo
            {
                MemberName = field.Name,
                ExplicitName = explicitName,
                Type = field.FieldType,
                Get = field.GetValue,
                Set = field.SetValue,
                IsRequired = IsRequiredMember(field),
                Order = field.GetCustomAttribute<PropertyOrderAttribute>(inherit: true)?.Order ?? 0,
                Condition = ignore?.Condition,
                Include = field.IsDefined(typeof(IncludeAttribute), inherit: true),
                IsExtensionData = isExtension,
                IsField = true,
            });
        }

        if (members.FindAll(static m => m.IsExtensionData).Count > 1)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlMultipleExtensionData, type));
        }

        return members.ToArray();
    }

    /// <summary>
    /// Determines whether a type can serve as an extension-data member, which must be assignable from
    /// <see cref="Dictionary{TKey, TValue}" /> of <see cref="string" /> to <see cref="object" />.
    /// </summary>
    /// <param name="type">The declared member type.</param>
    /// <returns><see langword="true" /> when the type is a supported extension-data container.</returns>
    private static bool IsExtensionDataCompatible(Type type) =>
        typeof(IDictionary<string, object?>).IsAssignableFrom(type);

    /// <summary>
    /// Determines whether a member is required, either through the <see langword="required" /> keyword (surfaced as
    /// <see cref="System.Runtime.CompilerServices.RequiredMemberAttribute" />) or through
    /// <see cref="RequiredAttribute" />.
    /// </summary>
    /// <param name="member">The reflected member.</param>
    /// <returns><see langword="true" /> when the member is required; otherwise <see langword="false" />.</returns>
    private static bool IsRequiredMember(MemberInfo member) =>
        member.IsDefined(typeof(System.Runtime.CompilerServices.RequiredMemberAttribute), inherit: false)
            || member.IsDefined(typeof(RequiredAttribute), inherit: false);
}
