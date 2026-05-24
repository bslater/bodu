// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseFormattingOptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Defines formatting options that influence how encoded output is generated from binary data using any positional
/// numeral system.
/// </summary>
/// <remarks>
/// <para>
/// These options can be combined to control character casing, spacing, prefix inclusion, and line formatting. The
/// effect of each option may vary by encoding; not every encoding honours every flag. Refer to the documentation of the
/// specific encoder for the supported subset.
/// </para>
/// <para>
/// <see cref="None" /> produces the most compact form for every encoder. Flag combinations that conflict (for example
/// <see cref="OmitPadding" /> on an encoding that does not emit padding) are silently ignored rather than rejected, so
/// consumers can compose options uniformly across encoders without conditional branching.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Compact lower-case hex (the default Base16 form).
/// string compact = Base16.Encode(data);
///
/// // Diagnostic-friendly: upper case, "0x" prefix, single-space byte groups.
/// string diagnostic = Base16.Encode(data,
///     BaseFormattingOptions.UpperCase
///     | BaseFormattingOptions.IncludePrefix
///     | BaseFormattingOptions.InsertSpacing);
///
/// // Padding-free Base64 — produces the same output as Base64Url.Encode.
/// string unpadded = Base64.Encode(data, BaseFormattingOptions.OmitPadding);
///]]>
/// </example>
[Flags]
public enum BaseFormattingOptions : byte
{
    /// <summary>
    /// No formatting is applied. The output is compact, lower case, and continuous with no spacing, prefix, or line
    /// breaks.
    /// </summary>
    None = 0,

    /// <summary>
    /// Formats the encoded output using upper case characters.
    /// </summary>
    UpperCase = 1 << 0,

    /// <summary>
    /// Inserts line breaks (<c>\r\n</c>) into the output at a fixed interval. The exact column at which breaks are
    /// inserted is encoding-specific.
    /// </summary>
    InsertLineBreaks = 1 << 1,

    /// <summary>
    /// Adds a standard prefix to the output denoting the encoding. The prefix format is encoding-specific (for example,
    /// <c>0x</c> for Base16).
    /// </summary>
    IncludePrefix = 1 << 2,

    /// <summary>
    /// Inserts a single space between adjacent groups of encoded symbols, typically aligned to byte boundaries.
    /// </summary>
    InsertSpacing = 1 << 3,

    /// <summary>
    /// Omits the trailing padding characters that the encoding specification would normally emit. This flag has no
    /// effect on encodings that do not use padding.
    /// </summary>
    OmitPadding = 1 << 4,
}
