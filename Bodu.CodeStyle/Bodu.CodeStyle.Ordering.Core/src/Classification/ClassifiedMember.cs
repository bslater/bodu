// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ClassifiedMember.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.Ordering;

/// <summary>
/// Represents the result of classifying an <see cref="IMemberInput" /> against the active
/// <see cref="MemberOrderOptions" />.
/// </summary>
public sealed class ClassifiedMember
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClassifiedMember" /> class.
    /// </summary>
    /// <param name="kind">The classified member kind.</param>
    /// <param name="accessibility">The accessibility rank of the member.</param>
    /// <param name="groupRank">The zero-based group rank, equal to the group's index within <see cref="MemberOrderOptions.MemberGroups" />.</param>
    /// <param name="accessibilityRank">The zero-based accessibility rank within <see cref="MemberOrderOptions.AccessibilityOrder" />.</param>
    public ClassifiedMember(MemberKind kind, AccessibilityRank accessibility, int groupRank, int accessibilityRank)
    {
        this.Kind = kind;
        this.Accessibility = accessibility;
        this.GroupRank = groupRank;
        this.AccessibilityRank = accessibilityRank;
    }

    /// <summary>
    /// Gets the classified member kind.
    /// </summary>
    /// <returns>The member kind reported by the classifier.</returns>
    public MemberKind Kind { get; }

    /// <summary>
    /// Gets the accessibility rank of the member.
    /// </summary>
    /// <returns>The accessibility rank.</returns>
    public AccessibilityRank Accessibility { get; }

    /// <summary>
    /// Gets the zero-based index of the member's group within <see cref="MemberOrderOptions.MemberGroups" />.
    /// </summary>
    /// <returns>The group rank; lower values appear earlier in the canonical order.</returns>
    public int GroupRank { get; }

    /// <summary>
    /// Gets the zero-based index of the member's accessibility within <see cref="MemberOrderOptions.AccessibilityOrder" />.
    /// </summary>
    /// <returns>The accessibility rank; lower values appear earlier in the canonical order.</returns>
    public int AccessibilityRank { get; }
}
