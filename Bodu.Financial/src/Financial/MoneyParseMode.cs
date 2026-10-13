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
    /// Accept only the <c>"&lt;ISO&gt; &lt;amount&gt;"</c> or <c>"&lt;amount&gt; &lt;ISO&gt;"</c> forms, with a
    /// registered, uppercase ISO code separated from the amount by one space, after trimming surrounding white space. A
    /// bare amount with no ISO code is rejected. This is the default.
    /// </summary>
    StrictIso = 0,

    /// <summary>
    /// Accept everything <see cref="StrictIso" /> accepts and, when no ISO code is present, a leading or trailing
    /// currency symbol that <see cref="MoneyParseOptions.CurrencyLookup" /> resolves to exactly one currency. The
    /// shipped catalogue carries no symbols, so symbol text parses only through a caller-supplied lookup that provides
    /// them.
    /// </summary>
    CultureAware = 1,

    /// <summary>
    /// Accept everything <see cref="StrictIso" /> accepts and a lower-case ISO code, which is normalised to upper case
    /// before resolution.
    /// </summary>
    LenientImport = 2,

    /// <summary>
    /// Accept only the invariant round-trip form produced by the <c>"R"</c> format specifier: the uppercase ISO code,
    /// one space, and the amount in invariant fixed-point notation (an optional leading minus sign, digits, and an
    /// optional decimal point followed by digits), with no surrounding white space.
    /// <see cref="MoneyParseOptions.FormatProvider" /> is ignored, and a scale finer than the currency's registered
    /// minor units is kept, so the parsed value equals the value that was formatted.
    /// </summary>
    RoundTripOnly = 3,
}
