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
/// </remarks>
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
