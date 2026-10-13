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
