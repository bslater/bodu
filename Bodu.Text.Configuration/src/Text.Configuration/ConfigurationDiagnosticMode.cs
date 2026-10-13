// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDiagnosticMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how the reader reacts to recoverable diagnostics - either throwing immediately, gathering them on the
/// resulting document, or silently ignoring them.
/// </summary>
public enum ConfigurationDiagnosticMode
{
    /// <summary>
    /// The first recoverable error throws a <see cref="ConfigurationParseException" /> whose
    /// <see cref="ConfigurationParseException.Diagnostics" /> contains the single offending diagnostic.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// The reader runs to completion, capturing diagnostics on the resulting parse result's diagnostics list, and never
    /// throws <see cref="ConfigurationParseException" />. An I/O error from the source still propagates.
    /// </summary>
    Collect = 1,

    /// <summary>
    /// Diagnostics are not retained: errors are silently dropped, and <see cref="ConfigurationParseException" /> is
    /// never thrown. An I/O error from the source still propagates. Useful for production hot paths where diagnostics
    /// would only add allocations.
    /// </summary>
    Ignore = 2,
}
