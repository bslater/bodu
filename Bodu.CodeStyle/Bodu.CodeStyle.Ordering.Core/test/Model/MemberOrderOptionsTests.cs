// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberOrderOptionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using Bodu.CodeStyle.Ordering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.Ordering.Test.Model;

[TestClass]
public sealed class MemberOrderOptionsTests
{
    /// <summary>
    /// Verifies that the constructor throws when the member-groups array is default.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMemberGroupsIsDefault_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new MemberOrderOptions(
                memberGroups: default,
                accessibilityOrder: ImmutableArray<AccessibilityRank>.Empty,
                modifierOrder: ImmutableArray<string>.Empty,
                blankLinesBetweenMembers: 1,
                blankLinesBetweenGroups: 1,
                preserveRegions: false,
                preservePartialTypeOrder: true,
                reorderAcrossPreprocessorDirectives: false);
        });

        Assert.AreEqual("memberGroups", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws when the accessibility-order array is default.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAccessibilityOrderIsDefault_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new MemberOrderOptions(
                memberGroups: ImmutableArray<MemberGroupRule>.Empty,
                accessibilityOrder: default,
                modifierOrder: ImmutableArray<string>.Empty,
                blankLinesBetweenMembers: 1,
                blankLinesBetweenGroups: 1,
                preserveRegions: false,
                preservePartialTypeOrder: true,
                reorderAcrossPreprocessorDirectives: false);
        });

        Assert.AreEqual("accessibilityOrder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws when the modifier-order array is default.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenModifierOrderIsDefault_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new MemberOrderOptions(
                memberGroups: ImmutableArray<MemberGroupRule>.Empty,
                accessibilityOrder: ImmutableArray<AccessibilityRank>.Empty,
                modifierOrder: default,
                blankLinesBetweenMembers: 1,
                blankLinesBetweenGroups: 1,
                preserveRegions: false,
                preservePartialTypeOrder: true,
                reorderAcrossPreprocessorDirectives: false);
        });

        Assert.AreEqual("modifierOrder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws when blank-lines-between-members is negative.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBlankLinesBetweenMembersIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MemberOrderOptions(
                memberGroups: ImmutableArray<MemberGroupRule>.Empty,
                accessibilityOrder: ImmutableArray<AccessibilityRank>.Empty,
                modifierOrder: ImmutableArray<string>.Empty,
                blankLinesBetweenMembers: -1,
                blankLinesBetweenGroups: 1,
                preserveRegions: false,
                preservePartialTypeOrder: true,
                reorderAcrossPreprocessorDirectives: false);
        });

        Assert.AreEqual("blankLinesBetweenMembers", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws when blank-lines-between-groups is negative.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBlankLinesBetweenGroupsIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MemberOrderOptions(
                memberGroups: ImmutableArray<MemberGroupRule>.Empty,
                accessibilityOrder: ImmutableArray<AccessibilityRank>.Empty,
                modifierOrder: ImmutableArray<string>.Empty,
                blankLinesBetweenMembers: 1,
                blankLinesBetweenGroups: -1,
                preserveRegions: false,
                preservePartialTypeOrder: true,
                reorderAcrossPreprocessorDirectives: false);
        });

        Assert.AreEqual("blankLinesBetweenGroups", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructor-supplied properties round-trip through the public getters.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldRoundTripProperties()
    {
        ImmutableArray<MemberGroupRule> groups = ImmutableArray.Create(
            new MemberGroupRule("X", ImmutableArray.Create(MemberKind.Method), ImmutableArray<string>.Empty));
        ImmutableArray<AccessibilityRank> access = ImmutableArray.Create(AccessibilityRank.Public);
        ImmutableArray<string> modifiers = ImmutableArray.Create("public", "static");

        MemberOrderOptions options = new MemberOrderOptions(
            memberGroups: groups,
            accessibilityOrder: access,
            modifierOrder: modifiers,
            blankLinesBetweenMembers: 2,
            blankLinesBetweenGroups: 3,
            preserveRegions: true,
            preservePartialTypeOrder: true,
            reorderAcrossPreprocessorDirectives: true);

        Assert.AreEqual(1, options.MemberGroups.Length);
        Assert.AreEqual("X", options.MemberGroups[0].Name);
        Assert.AreEqual(1, options.AccessibilityOrder.Length);
        Assert.AreEqual(2, options.ModifierOrder.Length);
        Assert.AreEqual(2, options.BlankLinesBetweenMembers);
        Assert.AreEqual(3, options.BlankLinesBetweenGroups);
        Assert.IsTrue(options.PreserveRegions);
        Assert.IsTrue(options.PreservePartialTypeOrder);
        Assert.IsTrue(options.ReorderAcrossPreprocessorDirectives);
    }
}
