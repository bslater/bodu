// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberOrderOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;

namespace Bodu.CodeStyle.Ordering;

/// <summary>
/// Provides the immutable policy that drives Bodu's member-ordering rules.
/// </summary>
/// <remarks>
/// <para>
/// This type carries the configuration only; a future milestone supplies the analyzer and code fix that consume
/// it. The intent of shipping the model early is to lock the JSON shape and the public surface so consumers can
/// author <c>bodu.memberorder.json</c> files ahead of the analyzer being available.
/// </para>
/// </remarks>
public sealed class MemberOrderOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberOrderOptions" /> class.
    /// </summary>
    /// <param name="memberGroups">The ordered list of member groups.</param>
    /// <param name="accessibilityOrder">The ordered list of accessibility ranks, most-visible first.</param>
    /// <param name="modifierOrder">The canonical order of modifier keywords used when normalising declarations.</param>
    /// <param name="blankLinesBetweenMembers">The number of blank lines required between adjacent members.</param>
    /// <param name="blankLinesBetweenGroups">The number of blank lines required between adjacent member groups.</param>
    /// <param name="preserveRegions">Whether <c>#region</c> blocks are preserved during reorders.</param>
    /// <param name="preservePartialTypeOrder">Whether partial-type files retain their existing member order.</param>
    /// <param name="reorderAcrossPreprocessorDirectives">Whether members may be moved across preprocessor directives.</param>
    public MemberOrderOptions(
        ImmutableArray<MemberGroupRule> memberGroups,
        ImmutableArray<AccessibilityRank> accessibilityOrder,
        ImmutableArray<string> modifierOrder,
        int blankLinesBetweenMembers,
        int blankLinesBetweenGroups,
        bool preserveRegions,
        bool preservePartialTypeOrder,
        bool reorderAcrossPreprocessorDirectives)
    {
        if (memberGroups.IsDefault) throw new ArgumentException("Member groups array must be initialised.", nameof(memberGroups));
        if (accessibilityOrder.IsDefault) throw new ArgumentException("Accessibility order array must be initialised.", nameof(accessibilityOrder));
        if (modifierOrder.IsDefault) throw new ArgumentException("Modifier order array must be initialised.", nameof(modifierOrder));
        if (blankLinesBetweenMembers < 0) throw new ArgumentOutOfRangeException(nameof(blankLinesBetweenMembers), "Value must not be negative.");
        if (blankLinesBetweenGroups < 0) throw new ArgumentOutOfRangeException(nameof(blankLinesBetweenGroups), "Value must not be negative.");

        this.MemberGroups = memberGroups;
        this.AccessibilityOrder = accessibilityOrder;
        this.ModifierOrder = modifierOrder;
        this.BlankLinesBetweenMembers = blankLinesBetweenMembers;
        this.BlankLinesBetweenGroups = blankLinesBetweenGroups;
        this.PreserveRegions = preserveRegions;
        this.PreservePartialTypeOrder = preservePartialTypeOrder;
        this.ReorderAcrossPreprocessorDirectives = reorderAcrossPreprocessorDirectives;
    }

    /// <summary>
    /// Gets the ordered list of member groups.
    /// </summary>
    /// <returns>The immutable list of group rules, in declaration order.</returns>
    public ImmutableArray<MemberGroupRule> MemberGroups { get; }

    /// <summary>
    /// Gets the ordered list of accessibility ranks, most-visible first.
    /// </summary>
    /// <returns>The immutable list of accessibility ranks.</returns>
    public ImmutableArray<AccessibilityRank> AccessibilityOrder { get; }

    /// <summary>
    /// Gets the canonical order of modifier keywords used when normalising declarations.
    /// </summary>
    /// <returns>The immutable list of modifier keywords in canonical order.</returns>
    public ImmutableArray<string> ModifierOrder { get; }

    /// <summary>
    /// Gets the number of blank lines required between adjacent members within a group.
    /// </summary>
    /// <returns>The configured blank-line count.</returns>
    public int BlankLinesBetweenMembers { get; }

    /// <summary>
    /// Gets the number of blank lines required between adjacent member groups.
    /// </summary>
    /// <returns>The configured blank-line count.</returns>
    public int BlankLinesBetweenGroups { get; }

    /// <summary>
    /// Gets a value indicating whether <c>#region</c> blocks are preserved during reorders.
    /// </summary>
    /// <returns><see langword="true" /> when regions are preserved; otherwise <see langword="false" />.</returns>
    public bool PreserveRegions { get; }

    /// <summary>
    /// Gets a value indicating whether partial-type files retain their existing member order.
    /// </summary>
    /// <returns><see langword="true" /> when partial types are not reordered across files; otherwise <see langword="false" />.</returns>
    public bool PreservePartialTypeOrder { get; }

    /// <summary>
    /// Gets a value indicating whether members may be moved across preprocessor directives.
    /// </summary>
    /// <returns><see langword="true" /> when crossing directives is permitted; otherwise <see langword="false" />.</returns>
    public bool ReorderAcrossPreprocessorDirectives { get; }
}
