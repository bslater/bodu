// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IFinancialServiceBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Defines the fluent registration surface returned by
/// <see cref="ServiceCollectionExtensions.AddFinancialService(IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration?, string)" />
/// , mirroring the <c>IHttpClientBuilder</c> / <c>IMvcBuilder</c> pattern.
/// </summary>
public interface IFinancialServiceBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection" /> that the builder is composing into.
    /// </summary>
    /// <returns>The underlying service collection.</returns>
    IServiceCollection Services { get; }
}
