// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlMemberInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Describes a serializable property or field discovered by reflection, with cached accessors and the metadata needed
/// to compute its YAML key.
/// </summary>
internal sealed class YamlMemberInfo
{
    private static readonly ConcurrentDictionary<Type, YamlMemberInfo[]> s_cache = new();

    /// <summary>
    /// Gets or sets the declared member name.
    /// </summary>
    /// <value>The CLR member name.</value>
    public required string MemberName { get; init; }

    /// <summary>
    /// Gets or sets the explicit YAML key supplied by an attribute, if any.
    /// </summary>
    /// <value>The explicit key, or <see langword="null" />.</value>
    public string? ExplicitName { get; init; }

    /// <summary>
    /// Gets or sets the member's value type.
    /// </summary>
    /// <value>The member type.</value>
    public required Type Type { get; init; }

    /// <summary>
    /// Gets or sets the accessor that reads the member's value from an instance.
    /// </summary>
    /// <value>The getter delegate.</value>
    public required Func<object, object?> Get { get; init; }

    /// <summary>
    /// Gets or sets the accessor that writes the member's value to an instance, when writable.
    /// </summary>
    /// <value>The setter delegate, or <see langword="null" /> when the member is read-only.</value>
    public Action<object, object?>? Set { get; init; }

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
        foreach (var member in members)
        {
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
        var all = s_cache.GetOrAdd(type, static t => Discover(t));
        if (includeFields)
            return all;

        return Array.FindAll(all, static m => !m.IsField);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the member is a field rather than a property.
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

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetMethod is null)
                continue;

            if (property.IsDefined(typeof(YamlIgnoreAttribute), inherit: true))
                continue;

            var explicitName = property.GetCustomAttribute<YamlPropertyNameAttribute>(inherit: true)?.Name;
            Action<object, object?>? setter = null;
            if (property.SetMethod is { IsPublic: true })
                setter = property.SetValue;

            members.Add(new YamlMemberInfo
            {
                MemberName = property.Name,
                ExplicitName = explicitName,
                Type = property.PropertyType,
                Get = property.GetValue,
                Set = setter,
                IsField = false,
            });
        }

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.IsDefined(typeof(YamlIgnoreAttribute), inherit: true))
                continue;

            var explicitName = field.GetCustomAttribute<YamlPropertyNameAttribute>(inherit: true)?.Name;
            members.Add(new YamlMemberInfo
            {
                MemberName = field.Name,
                ExplicitName = explicitName,
                Type = field.FieldType,
                Get = field.GetValue,
                Set = field.SetValue,
                IsField = true,
            });
        }

        return members.ToArray();
    }
}
