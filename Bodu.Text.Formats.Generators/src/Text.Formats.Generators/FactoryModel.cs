// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FactoryModel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Describes one annotated type for which a factory is generated: its identity, containment, members, and any
/// conditions that block or degrade generation.
/// </summary>
/// <param name="Kind">The factory kind being generated.</param>
/// <param name="Namespace">The containing namespace, or an empty string for the global namespace.</param>
/// <param name="TypeName">The simple name of the annotated type.</param>
/// <param name="ContainingTypes">
/// The declarations of the containing type chain, outermost first, each as the header text of a partial declaration
/// (for example <c>public partial class Outer</c>); empty for a non-nested type.
/// </param>
/// <param name="DeclarationHeader">
/// The partial declaration header of the annotated type itself (for example <c>public partial class Person</c>).
/// </param>
/// <param name="Accessibility">The accessibility keyword sequence for the generated factory property.</param>
/// <param name="IsPartial">Whether the annotated type (and every containing type) is declared partial.</param>
/// <param name="IsGeneric">Whether the annotated type or a containing type declares type parameters.</param>
/// <param name="Members">The mapped members in declaration order.</param>
/// <param name="SkippedMembers">The names and type displays of members skipped as unsupported.</param>
internal sealed record FactoryModel(
    FactoryKind Kind,
    string Namespace,
    string TypeName,
    EquatableArray<string> ContainingTypes,
    string DeclarationHeader,
    string Accessibility,
    bool IsPartial,
    bool IsGeneric,
    EquatableArray<MemberModel> Members,
    EquatableArray<string> SkippedMembers);
