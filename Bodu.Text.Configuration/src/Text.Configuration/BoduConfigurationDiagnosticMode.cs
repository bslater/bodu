// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDiagnosticMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how the reader reacts to recoverable diagnostics — either throwing immediately, gathering them on
/// the resulting document, or silently ignoring them.
/// </summary>
public enum BoduConfigurationDiagnosticMode
{
    /// <summary>
    /// The first recoverable error throws a <see cref="BoduConfigurationParseException" /> whose
    /// <see cref="BoduConfigurationParseException.Diagnostics" /> contains the single offending diagnostic.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// The reader runs to completion, capturing diagnostics on the resulting
    /// <see cref="BoduConfigurationDocument.Diagnostics" />. Non-recoverable errors (truncated stream,
    /// I/O exceptions) still throw.
    /// </summary>
    Collect = 1,

    /// <summary>
    /// Diagnostics are not retained. Recoverable errors are silently dropped; non-recoverable errors still
    /// throw. Useful for production hot paths where diagnostics would only add allocations.
    /// </summary>
    Ignore = 2,
}
