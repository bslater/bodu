// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OverrideOperation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Identifies the kind of override operation an <see cref="OverrideEntry" /> represents.
/// </summary>
internal enum OverrideOperation
{
    /// <summary>
    /// Adds a new rule to an existing concept.
    /// </summary>
    AddRule = 0,

    /// <summary>
    /// Patches selected sections of an existing rule.
    /// </summary>
    PatchRule = 1,

    /// <summary>
    /// Removes an existing rule from a concept.
    /// </summary>
    RemoveRule = 2,
}
