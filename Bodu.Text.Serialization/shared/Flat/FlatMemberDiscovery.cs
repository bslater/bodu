// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDiscovery.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bodu.Text.Serialization;

/// <summary>
/// Enumerates directly declared visible POCO members using DotEnv/INI member selection rules.
/// </summary>
internal static class FlatMemberDiscovery
{
    /// <summary>
    /// Enumerates publicly visible instance members using the flat-serializer selection rules.
    /// </summary>
    /// <param name="type">The type whose members will be discovered.</param>
    /// <param name="includeFields">Whether public instance fields are included.</param>
    /// <returns>The selected properties and, when requested, fields.</returns>
    internal static IEnumerable<MemberInfo> Enumerate(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        bool includeFields)
    {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length == 0 &&
                property.GetCustomAttribute<IgnoreAttribute>() is not { Condition: IgnoreCondition.Always })
                yield return property;
        }

        if (includeFields)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<IgnoreAttribute>() is not { Condition: IgnoreCondition.Always })
                    yield return field;
            }
        }
    }
}
