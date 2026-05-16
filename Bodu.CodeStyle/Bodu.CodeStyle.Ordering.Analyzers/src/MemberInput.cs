// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberInput.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.CodeStyle.Ordering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bodu.CodeStyle.Ordering.Analyzers;

/// <summary>
/// Adapts a Roslyn <see cref="MemberDeclarationSyntax" /> to the Roslyn-free <see cref="IMemberInput" />
/// abstraction consumed by <see cref="MemberClassifier" />.
/// </summary>
internal sealed class MemberInput : IMemberInput
{
    private MemberInput(
        string declarationKind,
        AccessibilityRank accessibility,
        bool isStatic,
        bool isReadOnly,
        bool isConst,
        bool isExplicitInterface)
    {
        this.DeclarationKind = declarationKind;
        this.Accessibility = accessibility;
        this.IsStatic = isStatic;
        this.IsReadOnly = isReadOnly;
        this.IsConst = isConst;
        this.IsExplicitInterface = isExplicitInterface;
    }

    /// <inheritdoc />
    public string DeclarationKind { get; }

    /// <inheritdoc />
    public AccessibilityRank Accessibility { get; }

    /// <inheritdoc />
    public bool IsStatic { get; }

    /// <inheritdoc />
    public bool IsReadOnly { get; }

    /// <inheritdoc />
    public bool IsConst { get; }

    /// <inheritdoc />
    public bool IsExplicitInterface { get; }

    /// <summary>
    /// Builds a <see cref="MemberInput" /> from the supplied member declaration.
    /// </summary>
    public static MemberInput FromSyntax(MemberDeclarationSyntax member)
    {
        if (member is null) throw new ArgumentNullException(nameof(member));

        var declarationKind = member.Kind().ToString();
        AccessibilityRank accessibility = ResolveAccessibility(member);
        var isStatic = HasModifier(member, SyntaxKind.StaticKeyword);
        var isReadOnly = HasModifier(member, SyntaxKind.ReadOnlyKeyword);
        var isConst = HasModifier(member, SyntaxKind.ConstKeyword);
        var isExplicitInterface = HasExplicitInterfaceSpecifier(member);

        return new MemberInput(declarationKind, accessibility, isStatic, isReadOnly, isConst, isExplicitInterface);
    }

    private static AccessibilityRank ResolveAccessibility(MemberDeclarationSyntax member)
    {
        SyntaxTokenList modifiers = member.Modifiers;
        var hasPublic = false;
        var hasProtected = false;
        var hasInternal = false;
        var hasPrivate = false;
        foreach (SyntaxToken token in modifiers)
        {
            switch (token.Kind())
            {
                case SyntaxKind.PublicKeyword: hasPublic = true; break;
                case SyntaxKind.ProtectedKeyword: hasProtected = true; break;
                case SyntaxKind.InternalKeyword: hasInternal = true; break;
                case SyntaxKind.PrivateKeyword: hasPrivate = true; break;
            }
        }

        if (hasPublic) return AccessibilityRank.Public;
        if (hasProtected && hasInternal) return AccessibilityRank.ProtectedInternal;
        if (hasPrivate && hasProtected) return AccessibilityRank.PrivateProtected;
        if (hasProtected) return AccessibilityRank.Protected;
        if (hasInternal) return AccessibilityRank.Internal;
        if (hasPrivate) return AccessibilityRank.Private;

        // Type member with no modifier defaults to private for classes/structs.
        return AccessibilityRank.Private;
    }

    private static bool HasModifier(MemberDeclarationSyntax member, SyntaxKind modifierKind)
    {
        foreach (SyntaxToken token in member.Modifiers)
        {
            if (token.IsKind(modifierKind)) return true;
        }

        return false;
    }

    private static bool HasExplicitInterfaceSpecifier(MemberDeclarationSyntax member) =>
        member switch
        {
            PropertyDeclarationSyntax p => p.ExplicitInterfaceSpecifier is not null,
            MethodDeclarationSyntax m => m.ExplicitInterfaceSpecifier is not null,
            IndexerDeclarationSyntax i => i.ExplicitInterfaceSpecifier is not null,
            EventDeclarationSyntax e => e.ExplicitInterfaceSpecifier is not null,
            _ => false,
        };
}
