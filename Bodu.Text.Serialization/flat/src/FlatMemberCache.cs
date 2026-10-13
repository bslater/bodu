// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Serialization;

/// <summary>
/// Caches, for one set of line-format serializer options, the member descriptors of each type the serializer maps.
/// </summary>
/// <remarks>
/// <para>
/// Each serializer's options own one cache and consult it only once they are read-only, so the naming policy and field
/// inclusion that shaped a type's descriptors cannot change after they are cached. The descriptors are immutable, so
/// every call and thread shares them, and a type's members are reflected over once per options instance rather than on
/// every call.
/// </para>
/// <para>
/// A type that maps two members to one name is not cached: every call reflects over it again and throws.
/// </para>
/// </remarks>
internal sealed class FlatMemberCache
{
    /// <summary>The cached descriptors, by the type they describe.</summary>
    private readonly ConcurrentDictionary<Type, FlatMemberDescriptor[]> _members = new();

    /// <summary>
    /// Gets the number of types whose descriptors are cached.
    /// </summary>
    internal int Count => _members.Count;

    /// <summary>
    /// Gets the mapped members of a type, ordered by the property-order attribute, from the cache or by discovering
    /// them.
    /// </summary>
    /// <param name="type">The type whose members to map.</param>
    /// <param name="includeFields">Whether public instance fields are mapped as well as properties.</param>
    /// <param name="namingPolicy">The naming policy that converts member names, or <see langword="null" />.</param>
    /// <param name="createDuplicateNameException">
    /// Creates the serializer's exception for a type that maps two members to one name, from the type and the name.
    /// </param>
    /// <returns>The ordered member descriptors, shared with every other caller.</returns>
    /// <exception cref="Exception">
    /// The exception <paramref name="createDuplicateNameException" /> creates, when two members of
    /// <paramref name="type" /> map to one name.
    /// </exception>
    [RequiresUnreferencedCode("Member discovery reflects over public properties and fields that trimming cannot statically determine.")]
    internal FlatMemberDescriptor[] GetMembers(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        bool includeFields,
        NamingPolicy? namingPolicy,
        Func<Type, string, Exception> createDuplicateNameException)
    {
        if (_members.TryGetValue(type, out FlatMemberDescriptor[]? cached))
            return cached;

        FlatMemberDescriptor[] members = FlatMemberDiscovery.Describe(type, includeFields, namingPolicy);
        if (FlatMemberDiscovery.FindDuplicateName(members) is { } duplicate)
            throw createDuplicateNameException(type, duplicate);

        return _members.GetOrAdd(type, members);
    }
}
