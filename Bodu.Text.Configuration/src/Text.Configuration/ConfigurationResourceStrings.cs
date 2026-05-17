// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResourceStrings.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

/// <summary>
/// Centralizes the diagnostic and exception message templates used by the configuration reader, writer, and
/// resolver so that wording and formatting remain consistent across the assembly.
/// </summary>
/// <remarks>
/// Messages are exposed as <see langword="const" /> strings rather than via a RESX-backed resource manager. The
/// configuration format does not currently require localized diagnostics; if localization is later added, callers
/// can replace this class without affecting public API. Key names follow the
/// <c>&lt;Group&gt;_&lt;ExceptionTypeShort&gt;_&lt;Condition&gt;</c> convention used by every RESX-backed
/// counterpart elsewhere in the solution.
/// </remarks>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Resource string identifiers follow the diagnostic-class_subject convention used by RESX-generated equivalents elsewhere in the solution.")]
internal static class ConfigurationResourceStrings
{
    /// <summary>An argument value contained an empty segment within a configuration key.</summary>
    internal const string Arg_Invalid_ConfigKeyEmptySegment = "A configuration key contains an empty segment.";

    /// <summary>An argument value contained an illegal control character in a configuration key.</summary>
    internal const string Arg_Invalid_ConfigKeyControlChar = "Configuration key contains an illegal control character at position {0}.";

    /// <summary>A stream argument did not support reading.</summary>
    internal const string Arg_Invalid_StreamNotReadable = "Stream does not support reading.";

    /// <summary>A stream argument did not support writing.</summary>
    internal const string Arg_Invalid_StreamNotWritable = "Stream does not support writing.";

    /// <summary>A duplicate key was rejected because <see cref="IniDuplicateKeyBehavior.Disallowed" /> was active.</summary>
    internal const string Format_Invalid_DuplicateKey = "Duplicate configuration key '{0}'.";

    /// <summary>A configuration property had a blank key before the <c>=</c> separator.</summary>
    internal const string Format_Invalid_EmptyKey = "Configuration property key cannot be empty.";

    /// <summary>A configuration section header was empty (<c>[]</c>).</summary>
    internal const string Format_Invalid_EmptySectionHeader = "Configuration section header cannot be empty.";

    /// <summary>An escape sequence in a value was malformed.</summary>
    internal const string Format_Invalid_InvalidEscape = "Invalid escape sequence '\\{0}' in configuration value.";

    /// <summary>A configuration key contained an illegal character.</summary>
    internal const string Format_Invalid_InvalidKeyCharacter = "Configuration key contains the illegal character '{0}'.";

    /// <summary>A configuration property key ran past the configured maximum length.</summary>
    internal const string Format_Invalid_KeyTooLong = "Configuration property key exceeds the maximum permitted length of {0} characters.";

    /// <summary>A configuration line ran past the configured maximum length.</summary>
    internal const string Format_Invalid_LineTooLong = "Configuration line exceeds the maximum permitted length of {0} characters.";

    /// <summary>A configuration property line did not contain an <c>=</c> separator.</summary>
    internal const string Format_Invalid_MissingEquals = "Configuration property line is missing the '=' separator.";

    /// <summary>A glob expression was malformed because an opening brace had no matching close.</summary>
    internal const string Format_Invalid_UnbalancedBrace = "Unbalanced brace in glob expression.";

    /// <summary>A glob expression was malformed because an opening bracket had no matching close.</summary>
    internal const string Format_Invalid_UnbalancedBracket = "Unbalanced character class bracket in glob expression.";

    /// <summary>A configuration section header was not terminated with a closing <c>]</c>.</summary>
    internal const string Format_Invalid_UnterminatedSectionHeader = "Configuration section header is not terminated by ']'.";

    /// <summary>Caller invoked a typed getter that found the key but could not coerce the raw value.</summary>
    internal const string Format_Invalid_ValueNotConvertible = "The configuration value for key '{0}' ('{1}') could not be converted to {2}.";

    /// <summary>The resolved configuration view did not contain the requested key.</summary>
    internal const string Op_Invalid_ConfigKeyNotPresent = "Configuration key '{0}' is not present in the resolved view.";

    /// <summary>Caller attempted to set a pattern on the synthetic preamble section.</summary>
    internal const string Op_Invalid_PreambleHasPattern = "The preamble section does not have a pattern; attempting to set one is not allowed.";

    /// <summary>Caller called <see cref="BoduConfigurationExtensions.Resolve(Bodu.Text.Formats.IniDocument, string?, BoduConfigurationResolveOptions?)" /> on a parsed-from-string document without supplying a path root.</summary>
    internal const string Op_Invalid_ResolveWithoutPathRoot = "Cannot resolve a configuration document that was parsed from a string without a path root supplied via BoduConfigurationResolveOptions.PathRoot.";
}
