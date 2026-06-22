// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial;

/// <summary>
/// Default <see cref="IFinancialServiceBuilder" /> implementation, a thin wrapper around an
/// <see cref="IServiceCollection" /> that the fluent extension methods in
/// <see cref="FinancialServiceBuilderExtensions" /> operate against.
/// </summary>
internal sealed class FinancialServiceBuilder
    : IFinancialServiceBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialServiceBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection that subsequent registrations contribute into.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public FinancialServiceBuilder(IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services);

        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }
}
