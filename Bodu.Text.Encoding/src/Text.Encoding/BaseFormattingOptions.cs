// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseFormattingOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Defines formatting options that influence how encoded output is generated from binary data using any positional
/// numeral system.
/// </summary>
/// <remarks>
/// These options can be combined to control character casing, spacing, prefix inclusion, and line formatting. The
/// effect of each option may vary by encoding; not every encoding honours every flag. Refer to the documentation of
/// the specific encoder for the supported subset.
/// </remarks>
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
    /// Adds a standard prefix to the output denoting the encoding. The prefix format is encoding-specific
    /// (for example, <c>0x</c> for Base16).
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
