// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDiagnosticCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Identifies a specific class of <see cref="ConfigurationDiagnostic" />. Codes are stable so that consumers may
/// suppress, filter, or test for particular conditions without inspecting the message text.
/// </summary>
public enum ConfigurationDiagnosticCode
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
    /// A duplicate key was encountered when <see cref="IniDuplicateKeyBehavior.Disallowed" /> was active.
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

    /// <summary>
    /// A duplicate section header was encountered when <see cref="IniDuplicateSectionBehavior.Disallowed" /> was
    /// active.
    /// </summary>
    DuplicateSection = 12,

    /// <summary>
    /// A glob expression's numeric range (<c>{n1..n2}</c>) would expand to more alternatives than the parser permits.
    /// </summary>
    NumericRangeTooLarge = 13,

    /// <summary>
    /// A configuration section header contained non-whitespace content after the closing <c>]</c> when the configured
    /// section-header mode did not permit trailing content.
    /// </summary>
    TrailingContentAfterSectionHeader = 14,

    /// <summary>
    /// A glob expression contained brace alternations nested deeper than the parser permits, guarding against
    /// pathological patterns whose recursion or expansion would exhaust resources.
    /// </summary>
    BraceNestingTooDeep = 15,

    /// <summary>
    /// A glob expression exceeded the maximum length the parser is willing to compile, guarding against pathological
    /// inputs that would allocate a multi-megabyte regex source.
    /// </summary>
    PatternTooLong = 16,
}
