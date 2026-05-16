// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberOrderAnalysis.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Bodu.CodeStyle.Ordering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bodu.CodeStyle.Ordering.Analyzers;

/// <summary>
/// Provides shared classification and ordering helpers used by the member-ordering analyzer and its
/// accompanying code fix.
/// </summary>
internal static class MemberOrderAnalysis
{
    /// <summary>
    /// Returns the order key for the supplied member declaration: <c>(GroupRank, AccessibilityRank,
    /// OriginalIndex)</c>. Lower tuples sort earlier.
    /// </summary>
    public static (int GroupRank, int AccessibilityRank, int OriginalIndex) ResolveOrderKey(
        MemberDeclarationSyntax member,
        int originalIndex,
        MemberClassifier classifier)
    {
        if (member is null) throw new ArgumentNullException(nameof(member));
        if (classifier is null) throw new ArgumentNullException(nameof(classifier));

        var input = MemberInput.FromSyntax(member);
        ClassifiedMember classified = classifier.Classify(input);
        return (classified.GroupRank, classified.AccessibilityRank, originalIndex);
    }

    /// <summary>
    /// Returns <see langword="true" /> when the supplied type declaration's members are already in the
    /// canonical Bodu order.
    /// </summary>
    public static bool IsOrdered(TypeDeclarationSyntax type, MemberClassifier classifier)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        if (classifier is null) throw new ArgumentNullException(nameof(classifier));

        IReadOnlyList<(int GroupRank, int AccessibilityRank, int OriginalIndex)> keys = ResolveAllKeys(type, classifier);
        for (var i = 1; i < keys.Count; i++)
        {
            if (CompareKeys(keys[i - 1], keys[i]) > 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the order keys for every member in the supplied type declaration, indexed by their original
    /// declaration order.
    /// </summary>
    public static IReadOnlyList<(int GroupRank, int AccessibilityRank, int OriginalIndex)> ResolveAllKeys(
        TypeDeclarationSyntax type,
        MemberClassifier classifier)
    {
        var keys = new List<(int, int, int)>(type.Members.Count);
        for (var i = 0; i < type.Members.Count; i++)
        {
            keys.Add(ResolveOrderKey(type.Members[i], i, classifier));
        }

        return keys;
    }

    /// <summary>
    /// Returns <see langword="true" /> when the supplied type declaration contains any preprocessor directive
    /// in its member declarations or surrounding trivia.
    /// </summary>
    public static bool HasPreprocessorDirectives(TypeDeclarationSyntax type)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));

        foreach (MemberDeclarationSyntax member in type.Members)
        {
            if (member.DescendantTrivia(descendIntoTrivia: true).Any(t => t.IsDirective))
            {
                return true;
            }
        }

        return false;
    }

    private static int CompareKeys(
        (int GroupRank, int AccessibilityRank, int OriginalIndex) left,
        (int GroupRank, int AccessibilityRank, int OriginalIndex) right)
    {
        var g = left.GroupRank.CompareTo(right.GroupRank);
        if (g != 0) return g;

        var a = left.AccessibilityRank.CompareTo(right.AccessibilityRank);
        if (a != 0) return a;

        return left.OriginalIndex.CompareTo(right.OriginalIndex);
    }
}
