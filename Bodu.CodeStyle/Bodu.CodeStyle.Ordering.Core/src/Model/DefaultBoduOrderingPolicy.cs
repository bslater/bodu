// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultBoduOrderingPolicy.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.CodeStyle.Ordering;

/// <summary>
/// Provides the canonical Bodu member-ordering policy. The defaults mirror the implicit ordering already used
/// throughout the Bodu codebase: constants, static fields, instance fields, constructors, events, properties,
/// methods, nested types, with public-before-private and static-before-instance tie-breaking.
/// </summary>
public static class DefaultBoduOrderingPolicy
{
    /// <summary>
    /// Creates a new <see cref="MemberOrderOptions" /> populated with the canonical Bodu defaults.
    /// </summary>
    /// <returns>A fresh options instance carrying the Bodu profile.</returns>
    public static MemberOrderOptions Create()
    {
        ImmutableArray<MemberGroupRule> groups = ImmutableArray.Create(
            new MemberGroupRule("Constants", ImmutableArray.Create(MemberKind.Constant), ImmutableArray<string>.Empty),
            new MemberGroupRule("StaticReadonlyFields", ImmutableArray.Create(MemberKind.StaticReadonlyField), ImmutableArray<string>.Empty),
            new MemberGroupRule("StaticFields", ImmutableArray.Create(MemberKind.StaticField), ImmutableArray<string>.Empty),
            new MemberGroupRule("InstanceFields", ImmutableArray.Create(MemberKind.InstanceField), ImmutableArray<string>.Empty),
            new MemberGroupRule("Constructors", ImmutableArray.Create(MemberKind.StaticConstructor, MemberKind.Constructor), ImmutableArray<string>.Empty),
            new MemberGroupRule("Finalizers", ImmutableArray.Create(MemberKind.Finalizer), ImmutableArray<string>.Empty),
            new MemberGroupRule("Delegates", ImmutableArray.Create(MemberKind.Delegate), ImmutableArray<string>.Empty),
            new MemberGroupRule("Events", ImmutableArray.Create(MemberKind.Event), ImmutableArray<string>.Empty),
            new MemberGroupRule("Properties", ImmutableArray.Create(MemberKind.Property, MemberKind.Indexer), ImmutableArray<string>.Empty),
            new MemberGroupRule("Methods", ImmutableArray.Create(MemberKind.Method, MemberKind.Operator, MemberKind.ConversionOperator), ImmutableArray<string>.Empty),
            new MemberGroupRule("ExplicitInterfaceMembers", ImmutableArray.Create(MemberKind.ExplicitInterfaceMember), ImmutableArray<string>.Empty),
            new MemberGroupRule("NestedTypes", ImmutableArray.Create(MemberKind.NestedType), ImmutableArray<string>.Empty));

        ImmutableArray<AccessibilityRank> accessibilityOrder = ImmutableArray.Create(
            AccessibilityRank.Public,
            AccessibilityRank.ProtectedInternal,
            AccessibilityRank.Protected,
            AccessibilityRank.Internal,
            AccessibilityRank.PrivateProtected,
            AccessibilityRank.Private,
            AccessibilityRank.NotApplicable);

        ImmutableArray<string> modifierOrder = ImmutableArray.Create(
            "public",
            "protected",
            "internal",
            "private",
            "static",
            "extern",
            "new",
            "virtual",
            "abstract",
            "sealed",
            "override",
            "readonly",
            "unsafe",
            "volatile",
            "async");

        return new MemberOrderOptions(
            memberGroups: groups,
            accessibilityOrder: accessibilityOrder,
            modifierOrder: modifierOrder,
            blankLinesBetweenMembers: 1,
            blankLinesBetweenGroups: 1,
            preserveRegions: false,
            preservePartialTypeOrder: true,
            reorderAcrossPreprocessorDirectives: false);
    }
}
