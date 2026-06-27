// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentDecodingOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Specifies relaxations applied by the <see cref="PercentEncoding" /> decoder over its strict RFC 3986 default.
/// </summary>
[Flags]
public enum PercentDecodingOptions : byte
{
    /// <summary>
    /// Strict decoding: a <c>%</c> must be followed by exactly two hexadecimal digits, otherwise the input is rejected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Copies a malformed percent sequence (a <c>%</c> not followed by two hexadecimal digits) literally as its ASCII
    /// bytes instead of rejecting it, matching WHATWG-style lenient percent-decoding. Valid escapes are unaffected.
    /// </summary>
    AllowInvalidPercentLiterals = 1 << 0,
}
