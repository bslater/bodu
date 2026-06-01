// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyParseMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies how <see cref="MoneyParseOptions" /> interprets monetary text.
/// </summary>
public enum MoneyParseMode
{
    /// <summary>
    /// Accept only the canonical <c>"&lt;ISO&gt; &lt;amount&gt;"</c> or <c>"&lt;amount&gt; &lt;ISO&gt;"</c> forms with a
    /// registered, uppercase ISO code. This is the default and matches the <see cref="IParsable{TSelf}" /> surface.
    /// </summary>
    StrictIso = 0,

    /// <summary>
    /// Accept user-facing input, resolving a currency symbol through the configured lookup when no ISO code is present.
    /// </summary>
    CultureAware = 1,

    /// <summary>
    /// Accept loosely formatted import data: surrounding white space is trimmed and a lower-case ISO code is normalised
    /// to upper case before resolution.
    /// </summary>
    LenientImport = 2,

    /// <summary>
    /// Accept only the invariant round-trip form produced by the <c>"R"</c> format specifier.
    /// </summary>
    RoundTripOnly = 3,
}
