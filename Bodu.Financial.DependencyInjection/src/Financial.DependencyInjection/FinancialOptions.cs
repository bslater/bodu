// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Bodu.Financial.Serialization;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Configuration-bindable options for the Bodu.Financial dependency-injection surface.
/// </summary>
public sealed class FinancialOptions
{
    /// <summary>
    /// Gets or sets the JSON serialization policy registered for financial types.
    /// </summary>
    /// <value>The configured policy; defaults to <see cref="FinancialJsonPolicy.Strict" />.</value>
    public FinancialJsonPolicy JsonPolicy { get; set; } = FinancialJsonPolicy.Strict;

    /// <summary>
    /// Gets or sets the default unknown-currency policy advertised to consumers.
    /// </summary>
    /// <value>The configured policy; defaults to <see cref="UnknownCurrencyPolicy.Reject" />.</value>
    public UnknownCurrencyPolicy UnknownCurrency { get; set; } = UnknownCurrencyPolicy.Reject;
}
