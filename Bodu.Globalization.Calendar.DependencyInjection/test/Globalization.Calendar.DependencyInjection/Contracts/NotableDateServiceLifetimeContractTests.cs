// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceLifetimeContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection.Contracts;

/// <summary>
/// Drives <see cref="ServiceLifetimeContractTests" /> against the
/// <see cref="ServiceCollectionExtensions.AddNotableDates(IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration?, string)" />
/// extension, asserting that <see cref="INotableDateService" /> resolves as a singleton from a freshly
/// built service provider.
/// </summary>
[TestClass]
public sealed class NotableDateServiceLifetimeContractTests : ServiceLifetimeContractTests
{
    /// <inheritdoc />
    protected override IReadOnlyList<ServiceRegistrationKat> Registrations { get; } =
    [
        new(
            Name: "AddNotableDates registers INotableDateService as singleton",
            Register: services => services.AddNotableDates(),
            ServiceType: typeof(INotableDateService),
            ExpectedLifetime: ServiceLifetime.Singleton,
            ShouldResolve: true),
    ];
}
