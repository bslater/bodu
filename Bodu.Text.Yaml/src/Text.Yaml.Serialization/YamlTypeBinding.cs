// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTypeBinding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Binds a type's discovered members to their YAML keys under one options instance: the wire names are computed and
/// validated once, incoming keys resolve through a dictionary with the options' configured case sensitivity, and the
/// write order is precomputed. Instances are cached on the frozen options, so the per-object name work the serializer
/// previously repeated — recomputing the naming policy per key comparison and re-validating uniqueness per object —
/// is paid once per type.
/// </summary>
/// <remarks>
/// This type intentionally mirrors the role of the Bencode/TOML <c>TypeMetadata</c> so a future migration onto the
/// shared <c>Bodu.Text.Serialization</c> core maps its members one-to-one.
/// </remarks>
internal sealed class YamlTypeBinding
{
    /// <summary>The member lookup keyed by wire name with the options' configured case sensitivity; the first declared member wins a case-insensitive collision.</summary>
    private readonly Dictionary<string, int> _byWireName;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlTypeBinding" /> class.
    /// </summary>
    /// <param name="members">The discovered members, in declaration order.</param>
    /// <param name="wireNames">The wire name of each member, parallel to <paramref name="members" />.</param>
    /// <param name="writeOrder">The member indices in write order.</param>
    /// <param name="extensionData">The extension-data member, or <see langword="null" />.</param>
    /// <param name="byWireName">The member lookup keyed by wire name.</param>
    private YamlTypeBinding(
        YamlMemberInfo[] members,
        string[] wireNames,
        int[] writeOrder,
        YamlMemberInfo? extensionData,
        Dictionary<string, int> byWireName)
    {
        Members = members;
        WireNames = wireNames;
        WriteOrder = writeOrder;
        ExtensionData = extensionData;
        _byWireName = byWireName;
    }

    /// <summary>
    /// Gets the discovered members, in declaration order.
    /// </summary>
    /// <value>The members.</value>
    internal YamlMemberInfo[] Members { get; }

    /// <summary>
    /// Gets the wire name of each member, parallel to <see cref="Members" />.
    /// </summary>
    /// <value>The wire names.</value>
    internal string[] WireNames { get; }

    /// <summary>
    /// Gets the member indices in write order: declaration order, unless any member declares a non-default
    /// <see cref="YamlMemberInfo.Order" />, in which case the indices are stably sorted by it.
    /// </summary>
    /// <value>The write-order indices.</value>
    internal int[] WriteOrder { get; }

    /// <summary>
    /// Gets the extension-data member, or <see langword="null" /> when the type declares none.
    /// </summary>
    /// <value>The extension-data member, or <see langword="null" />.</value>
    internal YamlMemberInfo? ExtensionData { get; }

    /// <summary>
    /// Attempts to resolve the member an incoming YAML key maps to.
    /// </summary>
    /// <param name="name">The key read from the input.</param>
    /// <param name="member">When this method returns <see langword="true" />, the matching member.</param>
    /// <param name="wireName">When this method returns <see langword="true" />, the member's canonical wire name.</param>
    /// <returns><see langword="true" /> when a member matches; otherwise <see langword="false" />.</returns>
    internal bool TryGetMember(string name, [NotNullWhen(true)] out YamlMemberInfo? member, [NotNullWhen(true)] out string? wireName)
    {
        if (_byWireName.TryGetValue(name, out int index))
        {
            member = Members[index];
            wireName = WireNames[index];
            return true;
        }

        member = null;
        wireName = null;
        return false;
    }

    /// <summary>
    /// Creates the binding for a type under the supplied options, computing every member's wire name once and
    /// validating their uniqueness.
    /// </summary>
    /// <param name="type">The type to bind.</param>
    /// <param name="options">The serializer options supplying the naming policy, field inclusion, and case sensitivity.</param>
    /// <returns>The binding.</returns>
    /// <exception cref="InvalidOperationException">Two members map to the same YAML key.</exception>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    internal static YamlTypeBinding Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        YamlSerializerOptions options)
    {
        YamlMemberInfo[] members = YamlMemberInfo.ForType(type, options.IncludeFields);
        string[] wireNames = new string[members.Length];
        var byWireName = new Dictionary<string, int>(
            members.Length,
            options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        YamlMemberInfo? extensionData = null;
        bool ordered = false;

        for (int i = 0; i < members.Length; i++)
        {
            YamlMemberInfo member = members[i];
            wireNames[i] = member.WireName(options);
            ordered |= member.Order != 0;

            if (member.IsExtensionData)
            {
                extensionData = member;
                continue;
            }

            if (!seen.Add(wireNames[i]))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_DuplicateWireName, type, wireNames[i]));
            }

            // Under case-insensitive matching the first declared member wins a colliding key, matching the
            // first-match linear scan this lookup replaces.
            byWireName.TryAdd(wireNames[i], i);
        }

        int[] writeOrder = new int[members.Length];
        for (int i = 0; i < writeOrder.Length; i++)
            writeOrder[i] = i;

        if (ordered)
            writeOrder = [.. writeOrder.OrderBy(index => members[index].Order)];

        return new YamlTypeBinding(members, wireNames, writeOrder, extensionData, byWireName);
    }
}
