// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseFormattingOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Text;

/// <summary>
/// Defines formatting options that influence how encoded output is generated from binary data using any positional numeral system.
/// </summary>
/// <remarks>
/// These options can be combined to control character casing, spacing, prefix inclusion, and line formatting. The behaviour of each
/// option may vary depending on the encoding implementation.
/// </remarks>
[Flags]
public enum BaseFormattingOptions : byte
{
	/// <summary>
	/// No formatting is applied. The output will be compact, lowercase, and continuous without any spacing, prefix, or line breaks.
	/// </summary>
	None = 0,

	/// <summary>
	/// Formats the encoded output using uppercase characters.
	/// </summary>
	UpperCase = 1 << 0,

	/// <summary>
	/// Inserts line breaks into the output at fixed intervals.
	/// </summary>
	InsertLineBreaks = 1 << 1,

	/// <summary>
	/// Adds a standard prefix to the output to denote the encoding or base. The prefix format depends on the implementation.
	/// </summary>
	IncludePrefix = 1 << 2,

	/// <summary>
	/// Inserts spacing between adjacent groups of encoded symbols, typically aligned to byte or digit boundaries.
	/// </summary>
	InsertSpacing = 1 << 3
}
