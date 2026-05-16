// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResourceStrings.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Configuration;

/// <summary>
/// Centralizes the diagnostic and exception message templates used by the configuration reader, writer, and
/// resolver so that wording and formatting remain consistent across the assembly.
/// </summary>
/// <remarks>
/// Messages are exposed as <see langword="const" /> strings rather than via a RESX-backed resource manager. The
/// configuration format does not currently require localized diagnostics; if localization is later added, callers
/// can replace this class without affecting public API.
/// </remarks>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Resource string identifiers follow the diagnostic-class_subject convention used by RESX-generated equivalents elsewhere in the solution.")]
internal static class ConfigurationResourceStrings
{
    /// <summary>A configuration property line did not contain an <c>=</c> separator.</summary>
    internal const string ParseException_MissingEquals = "Configuration property line is missing the '=' separator.";

    /// <summary>A configuration property had a blank key before the <c>=</c> separator.</summary>
    internal const string ParseException_EmptyKey = "Configuration property key cannot be empty.";

    /// <summary>A configuration section header was not terminated with a closing <c>]</c>.</summary>
    internal const string ParseException_UnterminatedSectionHeader = "Configuration section header is not terminated by ']'.";

    /// <summary>A configuration section header was empty (<c>[]</c>).</summary>
    internal const string ParseException_EmptySectionHeader = "Configuration section header cannot be empty.";

    /// <summary>A duplicate key was rejected because <see cref="BoduConfigurationDuplicateKeyMode.Reject" /> was active.</summary>
    internal const string ParseException_DuplicateKey = "Duplicate configuration key '{0}'.";

    /// <summary>An escape sequence in a value was malformed.</summary>
    internal const string ParseException_InvalidEscape = "Invalid escape sequence '\\{0}' in configuration value.";

    /// <summary>A configuration key contained an illegal character.</summary>
    internal const string ParseException_InvalidKeyCharacter = "Configuration key contains the illegal character '{0}'.";

    /// <summary>A glob expression was malformed because an opening brace had no matching close.</summary>
    internal const string ParseException_UnbalancedBrace = "Unbalanced brace in glob expression.";

    /// <summary>A glob expression was malformed because an opening bracket had no matching close.</summary>
    internal const string ParseException_UnbalancedBracket = "Unbalanced character class bracket in glob expression.";

    /// <summary>Caller attempted to set a pattern on the synthetic preamble section.</summary>
    internal const string InvalidOperation_PreambleHasPattern = "The preamble section does not have a pattern; attempting to set one is not allowed.";

    /// <summary>Caller called <see cref="BoduConfigurationDocument.Resolve(string?, BoduConfigurationResolveOptions?)" /> on a parsed-from-string document without supplying a path root.</summary>
    internal const string InvalidOperation_ResolveWithoutPathRoot = "Cannot resolve a configuration document that was parsed from a string without a path root supplied via BoduConfigurationResolveOptions.PathRoot.";

    /// <summary>Caller invoked a typed getter that found the key but could not coerce the raw value.</summary>
    internal const string FormatException_ValueNotConvertible = "The configuration value for key '{0}' ('{1}') could not be converted to {2}.";

    /// <summary>A configuration line ran past the configured maximum length.</summary>
    internal const string ParseException_LineTooLong = "Configuration line exceeds the maximum permitted length of {0} characters.";

    /// <summary>A configuration property key ran past the configured maximum length.</summary>
    internal const string ParseException_KeyTooLong = "Configuration property key exceeds the maximum permitted length of {0} characters.";
}
