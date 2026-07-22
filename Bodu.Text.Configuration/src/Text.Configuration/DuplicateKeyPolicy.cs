// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DuplicateKeyPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Specifies how the configuration parser resolves a key that appears more than once within the same section.
/// </summary>
public enum DuplicateKeyPolicy
{
    /// <summary>
    /// The last occurrence of a duplicated key wins; earlier values are discarded. This is the default.
    /// </summary>
    LastWins = 0,

    /// <summary>
    /// The first occurrence of a duplicated key is retained; subsequent occurrences are silently ignored.
    /// </summary>
    FirstWins = 1,

    /// <summary>
    /// Any duplicated key is rejected, surfacing as a <see cref="ConfigurationParseException" /> or a collected
    /// diagnostic depending on the active <see cref="ConfigurationDiagnosticMode" />.
    /// </summary>
    Disallowed = 2,
}
