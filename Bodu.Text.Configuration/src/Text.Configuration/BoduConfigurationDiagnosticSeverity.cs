// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDiagnosticSeverity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Classifies the severity of a <see cref="BoduConfigurationDiagnostic" /> emitted while reading or resolving a
/// configuration document.
/// </summary>
public enum BoduConfigurationDiagnosticSeverity
{
    /// <summary>
    /// Informational message that does not indicate a problem. Typically used for surfacing non-fatal observations such
    /// as ignored unknown options.
    /// </summary>
    Info = 0,

    /// <summary>
    /// A non-fatal warning. Parsing or resolution will continue, but the configuration may behave differently from what
    /// the author intended.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// A recoverable error. The document is still produced (when <see cref="BoduConfigurationDiagnosticMode.Collect" />
    /// is active), but a portion of the input could not be honored.
    /// </summary>
    Error = 2,
}
