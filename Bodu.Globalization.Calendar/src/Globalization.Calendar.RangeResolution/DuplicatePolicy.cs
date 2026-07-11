// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DuplicatePolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Determines how the loader treats two rules that resolve to the same full identity within a resource graph.
/// </summary>
/// <remarks>
/// <para>
/// The recommended default is <see cref="Error" />, which surfaces accidental rule collapse rather than hiding it.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The same concept emitted twice for one day (for example by overlapping rules):
/// // KeepFirst drops the later duplicate, Error fails the resolve. Set via the policy:
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
