// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DuplicatePolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Specifies how the loader treats two concepts, or two rules of one concept, that a document declares with the same
/// identifier.
/// </summary>
/// <remarks>
/// <para>
/// The loader applies the policy of the document it loads, when it loads it, in document order, to the concepts and
/// rules that document declares, before it composes the document's imports and applies its overrides. The policy
/// reaches no further: across imports the first import that supplies a concept wins and a local concept replaces an
/// imported one with the same identifier, an imported document's own policy is not applied, so two rules sharing an
/// identifier in an imported concept stay <c>BODU-CAL-DUP-RULE</c>, and an <c>AddRule</c> override whose rule
/// identifier already exists in its target concept is <c>BODU-CAL-DUP-RULE</c> under every value. Overrides apply to
/// the concept the policy keeps.
/// </para>
/// <para>
/// A value that settles duplicates reports nothing. A binary pack is written from a loaded resource, whose duplicates
/// the policy has already settled, so the policy needs no pack format version of its own.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Keep the first declaration of a concept or rule identifier and discard the later ones.
/// NotableDateDocumentBuilder.Create("demo")
///     .WithResolutionPolicy(p => p.WithDuplicatePolicy(DuplicatePolicy.KeepFirst));
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="ResolutionPolicy" /> <seealso href="../guides/calendar/identity-and-resolution.html">Rule identity,
/// priority, and observed-date resolution (guide)</seealso>
public enum DuplicatePolicy
{
    /// <summary>
    /// Report two concepts that share an identifier as <c>BODU-CAL-DUP-ND</c>, and two rules of one concept that share
    /// one as <c>BODU-CAL-DUP-RULE</c>, both load errors. This is the default.
    /// </summary>
    Error = 0,

    /// <summary>
    /// Keep the first declaration of a concept identifier, and within it the first declaration of a rule identifier,
    /// each at its own position, and discard the later declarations.
    /// </summary>
    KeepFirst,

    /// <summary>
    /// Keep the last declaration of a concept identifier, and within it the last declaration of a rule identifier, each
    /// at its own position, and discard the earlier declarations.
    /// </summary>
    KeepLast,

    /// <summary>
    /// Append the rules of each later declaration of a concept identifier to the first declaration, whose own fields
    /// are kept. Rules are not merged: two rules that share an identifier remain a <c>BODU-CAL-DUP-RULE</c> error.
    /// </summary>
    Merge,
}
