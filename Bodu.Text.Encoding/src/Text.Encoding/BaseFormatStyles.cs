// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseFormatStyles.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Specifies formatting styles that influence how base-encoded input strings are parsed during decoding.
/// </summary>
/// <remarks>
/// These options control the leniency of the decoder. Multiple flags may be combined using a bitwise OR. The default
/// of <see cref="None" /> requires strict, decoration-free input.
/// </remarks>
[Flags]
public enum BaseFormatStyles : byte
{
    /// <summary>
    /// Indicates strict parsing mode. The input must contain only valid alphabet characters and must not include any
    /// prefix or whitespace decoration.
    /// </summary>
    None = 0,

    /// <summary>
    /// Allows the parser to accept and ignore an optional <c>0x</c> or <c>0X</c> prefix at the beginning of the input.
    /// </summary>
    AllowPrefix = 1 << 0,

    /// <summary>
    /// Allows the parser to ignore ASCII whitespace characters ( <c>' '</c>, <c>'\t'</c>, <c>'\r'</c>, <c>'\n'</c>)
    /// anywhere in the input.
    /// </summary>
    IgnoreWhitespace = 1 << 1,
}
