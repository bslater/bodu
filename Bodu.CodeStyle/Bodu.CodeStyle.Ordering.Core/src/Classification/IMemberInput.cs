// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IMemberInput.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.Ordering;

/// <summary>
/// Provides a Roslyn-free abstraction over a member declaration that the <see cref="MemberClassifier" /> can
/// classify.
/// </summary>
/// <remarks>
/// <para>
/// Future Roslyn adapters wrap <c>MemberDeclarationSyntax</c> behind this interface so the Core classifier remains
/// reusable from CLI, VSIX, and unit-test contexts.
/// </para>
/// </remarks>
public interface IMemberInput
{
    /// <summary>
    /// Gets the syntactic declaration kind as a string identifier (for example <c>"FieldDeclaration"</c>,
    /// <c>"MethodDeclaration"</c>, <c>"PropertyDeclaration"</c>).
    /// </summary>
    /// <returns>The declaration-kind identifier reported by the adapter.</returns>
    string DeclarationKind { get; }

    /// <summary>
    /// Gets the accessibility rank of the member.
    /// </summary>
    /// <returns>The accessibility rank.</returns>
    AccessibilityRank Accessibility { get; }

    /// <summary>
    /// Gets a value indicating whether the member is declared <c>static</c>.
    /// </summary>
    /// <returns><see langword="true" /> when the member is static; otherwise <see langword="false" />.</returns>
    bool IsStatic { get; }

    /// <summary>
    /// Gets a value indicating whether the member is declared <c>readonly</c>.
    /// </summary>
    /// <returns><see langword="true" /> when the member is readonly; otherwise <see langword="false" />.</returns>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether the member is declared <c>const</c>.
    /// </summary>
    /// <returns><see langword="true" /> when the member is const; otherwise <see langword="false" />.</returns>
    bool IsConst { get; }

    /// <summary>
    /// Gets a value indicating whether the member is an explicit interface implementation.
    /// </summary>
    /// <returns><see langword="true" /> when the member is an explicit interface member; otherwise <see langword="false" />.</returns>
    bool IsExplicitInterface { get; }
}
