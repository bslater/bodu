// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DuplicatePolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Specifies how a resource asks for two rules that resolve to the same full identity to be treated. Reserved: the
/// value is stored but not yet applied.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Reserved.</strong> The value is read from a document's <c>duplicatePolicy</c> attribute, kept on
/// <see cref="ResolutionPolicy.DuplicatePolicy" />, and written to binary packs, but neither the loader nor the service
/// consults it, so every value behaves the same way: two concepts with the same identifier in one document, or two
/// rules with the same identifier in one concept, are load errors (<c>BODU-CAL-DUP-ND</c> / <c>BODU-CAL-DUP-RULE</c>),
/// and across imports the first import that supplies a concept wins and a local concept replaces an imported one with
/// the same identifier. Implementing the policy is tracked by
/// <see href="https://github.com/bslater/bodu/issues/1119">issue #1119</see>.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Accepted and stored, but not yet applied: duplicate identifiers are load errors whatever the value.
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
    /// Report a duplicate identity as a validation error.
    /// </summary>
    Error = 0,

    /// <summary>
    /// Keep the first rule encountered and discard later duplicates.
    /// </summary>
    KeepFirst,

    /// <summary>
    /// Keep the last rule encountered and discard earlier duplicates.
    /// </summary>
    KeepLast,

    /// <summary>
    /// Merge duplicate rules into a single rule.
    /// </summary>
    Merge,
}
