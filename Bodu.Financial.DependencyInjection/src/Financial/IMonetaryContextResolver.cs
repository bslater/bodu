// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IMonetaryContextResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial;

/// <summary>
/// Resolves the monetary contexts that <see cref="FinancialOptions" /> configures and <c>AddMonetaryContext</c>
/// registers, following configuration changes without a restart.
/// </summary>
/// <remarks>
/// <para>
/// <c>AddFinancialService</c> registers the resolver as a singleton. It builds its contexts from the current
/// <see cref="FinancialOptions" /> when it is first resolved and rebuilds them on every
/// <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}" /> change notification, so a reload of the bound
/// configuration changes what the next call returns. Settings that fail validation throw
/// <see cref="Microsoft.Extensions.Options.OptionsValidationException" /> when the resolver is first resolved; on a
/// reload they are logged, and the contexts built before stay in effect. A value the configuration binder cannot
/// convert, such as an unknown rounding name, fails in the options system before any rebuild, and the resolver keeps
/// the contexts it built before.
/// </para>
/// <para>
/// A name resolves to the context configured under it in <see cref="FinancialOptions.MonetaryContexts" />, compared
/// case-insensitively, and failing that to the context registered for it with <c>AddMonetaryContext</c>, compared
/// ordinally as keyed services are. A configured context therefore takes precedence over a registered one of the same
/// name, while the keyed service itself still resolves the registered context.
/// </para>
/// </remarks>
public interface IMonetaryContextResolver
{
    /// <summary>
    /// Gets the default monetary context.
    /// </summary>
    /// <value>
    /// The context <see cref="FinancialOptions.DefaultMonetaryContext" /> configures, or
    /// <see cref="MonetaryContext.Default" /> when it is <see langword="null" />.
    /// </value>
    MonetaryContext Default { get; }

    /// <summary>
    /// Returns the monetary context with the specified name.
    /// </summary>
    /// <param name="name">The context name.</param>
    /// <returns>The context configured under <paramref name="name" />, or else the one registered under it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is empty or white space.</exception>
    /// <exception cref="KeyNotFoundException">
    /// No context is configured or registered under <paramref name="name" />.
    /// </exception>
    MonetaryContext Get(string name);

    /// <summary>
    /// Attempts to return the monetary context with the specified name.
    /// </summary>
    /// <param name="name">The context name.</param>
    /// <param name="context">
    /// When this method returns <see langword="true" />, the context configured under <paramref name="name" />, or else
    /// the one registered under it; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a context is configured or registered under <paramref name="name" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
    bool TryGet(string name, [NotNullWhen(true)] out MonetaryContext? context);
}
