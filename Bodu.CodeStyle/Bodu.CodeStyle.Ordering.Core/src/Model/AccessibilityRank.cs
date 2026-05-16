// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AccessibilityRank.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.Ordering;

/// <summary>
/// Identifies the accessibility of a type member for ordering purposes.
/// </summary>
public enum AccessibilityRank
{
    /// <summary>The member is <c>public</c>.</summary>
    Public = 0,

    /// <summary>The member is <c>protected internal</c>.</summary>
    ProtectedInternal = 1,

    /// <summary>The member is <c>protected</c>.</summary>
    Protected = 2,

    /// <summary>The member is <c>internal</c>.</summary>
    Internal = 3,

    /// <summary>The member is <c>private protected</c>.</summary>
    PrivateProtected = 4,

    /// <summary>The member is <c>private</c>.</summary>
    Private = 5,

    /// <summary>The accessibility does not apply (e.g. an explicit interface member).</summary>
    NotApplicable = 6,
}
