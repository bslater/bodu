// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberClassifier.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;

namespace Bodu.CodeStyle.Ordering;

/// <summary>
/// Classifies an <see cref="IMemberInput" /> instance into a <see cref="ClassifiedMember" /> describing its kind,
/// accessibility, and rank under the active <see cref="MemberOrderOptions" />.
/// </summary>
public sealed class MemberClassifier
{
    private readonly MemberOrderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberClassifier" /> class with the supplied options.
    /// </summary>
    /// <param name="options">The active member-ordering options.</param>
    public MemberClassifier(MemberOrderOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        this._options = options;
    }

    /// <summary>
    /// Classifies the supplied member.
    /// </summary>
    /// <param name="input">The member to classify.</param>
    /// <returns>The classified member, including its computed group rank and accessibility rank.</returns>
    public ClassifiedMember Classify(IMemberInput input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));

        MemberKind kind = DetermineKind(input);
        var groupRank = FindGroupRank(kind);
        var accessibilityRank = FindAccessibilityRank(input.Accessibility);

        return new ClassifiedMember(kind, input.Accessibility, groupRank, accessibilityRank);
    }

    private static MemberKind DetermineKind(IMemberInput input)
    {
        switch (input.DeclarationKind)
        {
            case "FieldDeclaration":
                if (input.IsConst) return MemberKind.Constant;
                if (input.IsStatic && input.IsReadOnly) return MemberKind.StaticReadonlyField;
                if (input.IsStatic) return MemberKind.StaticField;
                return MemberKind.InstanceField;

            case "ConstructorDeclaration":
                return input.IsStatic ? MemberKind.StaticConstructor : MemberKind.Constructor;

            case "DestructorDeclaration":
                return MemberKind.Finalizer;

            case "DelegateDeclaration":
                return MemberKind.Delegate;

            case "EventDeclaration":
            case "EventFieldDeclaration":
                return MemberKind.Event;

            case "PropertyDeclaration":
                return input.IsExplicitInterface ? MemberKind.ExplicitInterfaceMember : MemberKind.Property;

            case "IndexerDeclaration":
                return input.IsExplicitInterface ? MemberKind.ExplicitInterfaceMember : MemberKind.Indexer;

            case "MethodDeclaration":
                return input.IsExplicitInterface ? MemberKind.ExplicitInterfaceMember : MemberKind.Method;

            case "OperatorDeclaration":
                return MemberKind.Operator;

            case "ConversionOperatorDeclaration":
                return MemberKind.ConversionOperator;

            case "ClassDeclaration":
            case "StructDeclaration":
            case "RecordDeclaration":
            case "RecordStructDeclaration":
            case "InterfaceDeclaration":
            case "EnumDeclaration":
                return MemberKind.NestedType;

            default:
                return MemberKind.Method;
        }
    }

    private int FindGroupRank(MemberKind kind)
    {
        ImmutableArray<MemberGroupRule> groups = this._options.MemberGroups;
        for (var i = 0; i < groups.Length; i++)
        {
            if (groups[i].Kinds.Contains(kind))
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private int FindAccessibilityRank(AccessibilityRank accessibility)
    {
        ImmutableArray<AccessibilityRank> ranks = this._options.AccessibilityOrder;
        for (var i = 0; i < ranks.Length; i++)
        {
            if (ranks[i] == accessibility)
            {
                return i;
            }
        }

        return int.MaxValue;
    }
}
