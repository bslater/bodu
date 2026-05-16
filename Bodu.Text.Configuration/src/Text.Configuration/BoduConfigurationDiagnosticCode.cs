// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDiagnosticCode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Identifies a specific class of <see cref="BoduConfigurationDiagnostic" />. Codes are stable so that
/// consumers may suppress, filter, or test for particular conditions without inspecting the message text.
/// </summary>
public enum BoduConfigurationDiagnosticCode
{
    /// <summary>
    /// No specific code is associated with the diagnostic.
    /// </summary>
    None = 0,

    /// <summary>
    /// A property line did not contain a key/value separator (<c>=</c>).
    /// </summary>
    MissingEquals = 1,

    /// <summary>
    /// A property declared an empty key.
    /// </summary>
    EmptyKey = 2,

    /// <summary>
    /// A duplicate key was encountered when <see cref="BoduConfigurationDuplicateKeyMode.Reject" /> was active.
    /// </summary>
    DuplicateKey = 3,

    /// <summary>
    /// A section header opened with <c>[</c> but did not close with <c>]</c>.
    /// </summary>
    UnterminatedSectionHeader = 4,

    /// <summary>
    /// A section header contained no characters between <c>[</c> and <c>]</c>.
    /// </summary>
    EmptySectionHeader = 5,

    /// <summary>
    /// A glob expression contained an opening brace with no matching close.
    /// </summary>
    UnbalancedBrace = 6,

    /// <summary>
    /// A glob expression contained an opening bracket with no matching close.
    /// </summary>
    UnbalancedBracket = 7,

    /// <summary>
    /// A property key included an illegal character.
    /// </summary>
    InvalidKeyCharacter = 8,

    /// <summary>
    /// An escape sequence in a value was malformed.
    /// </summary>
    InvalidEscape = 9,

    /// <summary>
    /// A configuration line was longer than the configured maximum.
    /// </summary>
    LineTooLong = 10,

    /// <summary>
    /// A configuration property key was longer than the configured maximum.
    /// </summary>
    KeyTooLong = 11,
}
