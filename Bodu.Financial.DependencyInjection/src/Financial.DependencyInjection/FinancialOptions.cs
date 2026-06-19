// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Serialization;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Configuration-bindable options for the Bodu.Financial dependency-injection surface.
/// </summary>
public sealed class FinancialOptions
{
    /// <summary>
    /// Gets or sets the JSON serialization policy applied to the financial JSON options that <c>AddBoduFinancial</c>
    /// registers, unless a later <c>AddFinancialJson</c> overrides it.
    /// </summary>
    /// <value>The configured policy; defaults to <see cref="FinancialJsonPolicy.Strict" />.</value>
    public FinancialJsonPolicy JsonPolicy { get; set; } = FinancialJsonPolicy.Strict;
}
