// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilder.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection;

/// <summary>
/// Provides the default <see cref="INotableDateServiceBuilder" /> implementation, a thin wrapper around an
/// <see cref="IServiceCollection" /> that the fluent extension methods in
/// <see cref="NotableDateServiceBuilderExtensions" /> operate against.
/// </summary>
/// <remarks>
/// <para>
/// The type is internal to the assembly — consumers see only the <see cref="INotableDateServiceBuilder" /> interface
/// returned from
/// <see cref="ServiceCollectionExtensions.AddNotableDates(IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration?, string)" />
/// .
/// </para>
/// </remarks>
internal sealed class NotableDateServiceBuilder : INotableDateServiceBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateServiceBuilder" /> class wrapping the supplied service
    /// collection.
    /// </summary>
    /// <param name="services">The service collection that subsequent registrations contribute into.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    public NotableDateServiceBuilder(IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services);

        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }
}
