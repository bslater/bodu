// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDuplicateSectionBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Specifies how the INI document model resolves a section name that appears more than once.
/// </summary>
/// <remarks>
/// This is the central dialect divergence for INI: Windows profile APIs and many parsers merge repeated sections, while
/// Python's <c>configparser</c> (in strict mode) rejects them. The behaviour is configurable so a consumer can pin the
/// dialect it targets.
/// </remarks>
public enum IniDuplicateSectionBehavior
{
    /// <summary>
    /// Repeated sections are merged into one, with later keys appended (and duplicate keys resolved by the key policy).
    /// </summary>
    Merge = 0,

    /// <summary>
    /// A repeated section name throws <see cref="IniFormatException" />.
    /// </summary>
    Disallowed,
}
