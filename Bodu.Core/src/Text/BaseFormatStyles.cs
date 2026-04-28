// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseFormatStyles.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Text;

/// <summary>
/// Specifies formatting styles that influence how base-encoded input strings are parsed during decoding.
/// </summary>
/// <remarks>
/// These options are primarily used when decoding hexadecimal strings, allowing flexibility in how decorated input is interpreted.
/// Multiple flags can be combined using a bitwise OR operation.
/// </remarks>
[Flags]
public enum BaseFormatStyles : byte
{
	/// <summary>
	/// Indicates strict parsing mode. Input must contain only valid base characters (e.g., hexadecimal digits) and must not include any
	/// prefix or whitespace.
	/// </summary>
	None = 0,

	/// <summary>
	/// Allows the parser to accept and ignore an optional <c>0x</c> or <c>0X</c> prefix at the beginning of the input.
	/// </summary>
	AllowPrefix = 1 << 0,

	/// <summary>
	/// Allows the parser to ignore ASCII whitespace characters ( <c>' '</c>, <c>'\t'</c>, <c>'\r'</c>, <c>'\n'</c>) anywhere in the input.
	/// </summary>
	IgnoreWhitespace = 1 << 1
}
