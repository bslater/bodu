// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Configuration-bindable options for the Bodu.Financial dependency-injection surface: the default and named monetary
/// contexts that <see cref="IMonetaryContextResolver" /> serves.
/// </summary>
/// <remarks>
/// <para>
/// <c>AddFinancialService</c> binds the type from its configuration section, <c>"Financial"</c> by default, and the
/// resolver follows the bound value through <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}" />: when
/// the configuration reloads, the contexts it serves are rebuilt from the new settings, and settings that fail
/// validation leave the previous contexts in place. A section of the following shape configures a default context and
/// two named ones.
/// </para>
/// <example>
/// <code language="json">
///<![CDATA[
/// {
///   "Financial": {
///     "DefaultMonetaryContext": { "Rounding": "AwayFromZero" },
///     "MonetaryContexts": {
///       "Invoicing": { "ScalePolicy": "Custom", "CustomScale": 4 },
///       "CashDesk": { "CashRounding": "CurrencyCashIncrement" }
///     }
///   }
/// }
///]]>
/// </code>
/// </example>
/// <para>
/// JSON serialization options live in the companion <c>Bodu.Financial.Serialization.Json</c> package and are registered
/// via its <c>AddFinancialJson</c> extension.
/// </para>
/// </remarks>
public sealed class FinancialOptions
{
    /// <summary>
    /// Gets or sets the settings of the default monetary context.
    /// </summary>
    /// <value>
    /// The default context's settings, or <see langword="null" />, the default, for
    /// <see cref="MonetaryContext.Default" />.
    /// </value>
    public MonetaryContextOptions? DefaultMonetaryContext { get; set; }

    /// <summary>
    /// Gets the settings of the named monetary contexts, keyed by name.
    /// </summary>
    /// <value>
    /// The named contexts' settings, empty by default. Names compare case-insensitively, as configuration keys do.
    /// </value>
    public IDictionary<string, MonetaryContextOptions> MonetaryContexts { get; } =
        new Dictionary<string, MonetaryContextOptions>(StringComparer.OrdinalIgnoreCase);
}
