// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Serialization;

/// <summary>
/// Selects the JSON serialization shape and parsing strictness applied by the <see cref="Bodu.Financial" /> JSON
/// converters.
/// </summary>
/// <remarks>
/// <para>
/// The policy is supplied either by constructing a converter directly (e.g.
/// <c>new MoneyOfTCurrencyJsonConverter&lt;USD&gt;(FinancialJsonPolicy.Compact)</c>) or, more commonly, through
/// <see cref="FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters" /> which registers a coherent set of
/// converters on a <see cref="System.Text.Json.JsonSerializerOptions" />.
/// </para>
/// <para>
/// The shipped <c>[JsonConverter]</c> attributes on <see cref="Bodu.Financial.Money{TCurrency}" />,
/// <see cref="Bodu.Financial.Money" />, and <see cref="Bodu.Financial.MoneyBag" /> default to <see cref="Strict" />.
/// Consumers who need a different shape register their own policy through
/// <see cref="FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters" />; converters registered on
/// <see cref="System.Text.Json.JsonSerializerOptions.Converters" /> take precedence over the type-level attribute.
/// </para>
/// </remarks>
public enum FinancialJsonPolicy
{
    /// <summary>
    /// Canonical JSON shape suitable for ledger, persistence, and audit data: monetary values serialize as objects with
    /// <c>"amount"</c> and <c>"currency"</c> properties; <see cref="Bodu.Financial.MoneyBag" /> serializes as an object
    /// containing a <c>"balances"</c> map. Property names compare case-insensitively, duplicate properties are
    /// rejected, unknown properties are ignored, and a currency code that does not match <c>TCurrency.IsoCode</c> is
    /// rejected.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Same on-the-wire shape as <see cref="Strict" />, but additionally normalizes lowercase ISO currency codes to
    /// uppercase and trims surrounding whitespace before validation. Intended for import workflows that ingest data
    /// from external feeds and spreadsheets. Not suitable as a canonical storage shape.
    /// </summary>
    Lenient = 1,

    /// <summary>
    /// Compact JSON shape: <see cref="Bodu.Financial.Money{TCurrency}" /> and <see cref="Bodu.Financial.Money" />
    /// serialize as a single JSON string of the form <c>"&lt;amount&gt; &lt;ISO&gt;"</c> (for example
    /// <c>"19.99 USD"</c>); <see cref="Bodu.Financial.MoneyBag" /> serializes as a flat object mapping ISO codes to
    /// amounts (for example <c>{ "USD": 19.99, "EUR": 12.34 }</c>). Reads accept either ISO-prefix or ISO-suffix string
    /// forms and reuse the type's <c>TryParse</c> path for the numeric component.
    /// </summary>
    Compact = 2,
}
