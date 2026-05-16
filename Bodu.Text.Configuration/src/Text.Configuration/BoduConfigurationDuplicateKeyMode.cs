// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDuplicateKeyMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Selects how the reader treats two properties that share the same key within the same section.
/// </summary>
/// <remarks>
/// A future release may add a <c>KeepAll</c> mode that retains every duplicate as a separate accessible
/// property; v1 deliberately omits it because the document model lacks multi-value accessors.
/// </remarks>
public enum BoduConfigurationDuplicateKeyMode
{
    /// <summary>
    /// The later occurrence supersedes the earlier one. The earlier property remains in
    /// <see cref="BoduConfigurationSection.Properties" /> but is shadowed during resolution. This matches the
    /// EditorConfig "top-to-bottom precedence" rule and is the default.
    /// </summary>
    LastWins = 0,

    /// <summary>
    /// The first occurrence wins. Subsequent occurrences are ignored for resolution but remain in the document
    /// model so round-trips do not silently delete authored content.
    /// </summary>
    FirstWins = 1,

    /// <summary>
    /// A duplicate key is treated as a recoverable error. A diagnostic is emitted; if
    /// <see cref="BoduConfigurationDiagnosticMode.Throw" /> is active a <see cref="BoduConfigurationParseException" />
    /// is raised.
    /// </summary>
    Reject = 2,
}
