// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDiscovery.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bodu.Text.Serialization;

/// <summary>
/// Enumerates the public instance properties and fields, inherited ones included, that the Delimited, DotEnv and INI
/// serializers map.
/// </summary>
internal static class FlatMemberDiscovery
{
    /// <summary>
    /// Enumerates publicly visible instance members using the flat-serializer selection rules.
    /// </summary>
    /// <param name="type">The type whose members will be discovered.</param>
    /// <param name="includeFields">Whether public instance fields are included.</param>
    /// <returns>The selected properties and, when requested, fields.</returns>
    /// <remarks>
    /// Each member name is mapped once, to its most derived declaration: a property or field hidden with the
    /// <see langword="new" /> modifier is not returned, and the declaration that hides it takes the position at which
    /// the name first appeared. When that most derived declaration is ignored, the name is not returned at all. An
    /// indexer is never a member.
    /// </remarks>
    internal static IEnumerable<MemberInfo> Enumerate(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        bool includeFields)
    {
        // Reflection returns a member hidden with new beside the member that hides it, so only the most derived
        // declaration of each name is kept, at the position its name first appeared.
        var declarations = new List<MemberInfo>();
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length == 0)
                KeepMostDerived(declarations, property);
        }

        if (includeFields)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                KeepMostDerived(declarations, field);
        }

        foreach (MemberInfo declaration in declarations)
        {
            if (declaration.GetCustomAttribute<IgnoreAttribute>() is not { Condition: IgnoreCondition.Always })
                yield return declaration;
        }
    }

    /// <summary>
    /// Finds the first name that more than one of the mapped members uses, comparing names ordinally.
    /// </summary>
    /// <param name="members">The mapped members.</param>
    /// <returns>The first name mapped more than once, or <see langword="null" /> when every name is distinct.</returns>
    /// <remarks>
    /// Two members mapped to one name cannot be written as a document that reads back, so each serializer rejects the
    /// type before it reads or writes anything. Names that differ only in case are distinct, as they are on the wire.
    /// </remarks>
    internal static string? FindDuplicateName(IReadOnlyList<FlatMemberDescriptor> members)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (FlatMemberDescriptor member in members)
        {
            if (!names.Add(member.Name))
                return member.Name;
        }

        return null;
    }

    /// <summary>
    /// Adds a member declaration to the list unless the list already holds the same name from a more derived type, and
    /// replaces, in place, a declaration of the same name that it hides.
    /// </summary>
    /// <param name="declarations">The declarations kept so far, one per name.</param>
    /// <param name="candidate">The declaration to consider.</param>
    private static void KeepMostDerived(List<MemberInfo> declarations, MemberInfo candidate)
    {
        int existing = declarations.FindIndex(declaration => string.Equals(declaration.Name, candidate.Name, StringComparison.Ordinal));
        if (existing < 0)
        {
            declarations.Add(candidate);
            return;
        }

        if (candidate.DeclaringType is { } declaringType &&
            declarations[existing].DeclaringType is { } keptType &&
            declaringType.IsSubclassOf(keptType))
        {
            declarations[existing] = candidate;
        }
    }
}
