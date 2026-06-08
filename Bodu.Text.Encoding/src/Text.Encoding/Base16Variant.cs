// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Variant.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Identifies the hexadecimal alphabet case used by <see cref="Base16" />.
/// </summary>
/// <remarks>
/// Base16 encodes each byte as two hexadecimal characters. The variants differ only in the case of the alphabetic
/// digits <c>a</c>–<c>f</c>; both decode identically because hexadecimal parsing is case-insensitive. The same case
/// selection can also be expressed through <see cref="BaseFormattingOptions.UpperCase" /> on the option-based
/// overloads; the variant overloads provide the uniform <c>(bytes, variant, options)</c> shape shared with the other
/// Base-N families, and the variant takes precedence over the option for the alphabet case.
/// </remarks>
public enum Base16Variant : byte
{
    /// <summary>
    /// The canonical lower-case alphabet <c>0123456789abcdef</c>. This is the default and matches the output of the
    /// option-based overloads when <see cref="BaseFormattingOptions.UpperCase" /> is not set.
    /// </summary>
    Lower = 0,

    /// <summary>
    /// The upper-case alphabet <c>0123456789ABCDEF</c>, equivalent to setting
    /// <see cref="BaseFormattingOptions.UpperCase" /> on the option-based overloads.
    /// </summary>
    Upper = 1,
}
