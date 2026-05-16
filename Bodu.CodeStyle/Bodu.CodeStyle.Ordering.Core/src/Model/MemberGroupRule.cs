// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberGroupRule.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;

namespace Bodu.CodeStyle.Ordering;

/// <summary>
/// Defines a single ordered group of members for the member-ordering policy.
/// </summary>
public sealed class MemberGroupRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberGroupRule" /> class.
    /// </summary>
    /// <param name="name">The display name of the group.</param>
    /// <param name="kinds">The member kinds that belong to this group.</param>
    /// <param name="requiredModifiers">The optional set of modifiers a member must carry to belong to this group.</param>
    public MemberGroupRule(string name, ImmutableArray<MemberKind> kinds, ImmutableArray<string> requiredModifiers)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));
        if (kinds.IsDefault) throw new ArgumentException("Kinds array must be initialised.", nameof(kinds));
        if (requiredModifiers.IsDefault) throw new ArgumentException("Required modifiers array must be initialised.", nameof(requiredModifiers));

        this.Name = name;
        this.Kinds = kinds;
        this.RequiredModifiers = requiredModifiers;
    }

    /// <summary>
    /// Gets the display name of the group.
    /// </summary>
    /// <returns>The configured group name.</returns>
    public string Name { get; }

    /// <summary>
    /// Gets the member kinds that belong to this group.
    /// </summary>
    /// <returns>The immutable list of member kinds.</returns>
    public ImmutableArray<MemberKind> Kinds { get; }

    /// <summary>
    /// Gets the optional set of modifiers a member must carry to belong to this group.
    /// </summary>
    /// <returns>The immutable list of required modifier keywords, or an empty list when no modifiers are required.</returns>
    public ImmutableArray<string> RequiredModifiers { get; }
}
