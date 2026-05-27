// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceLifetimeContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection.Contracts;

/// <summary>
/// Reusable behavioural contract test base for dependency-injection service-registration scenarios.
/// Concrete subclasses override <see cref="Registrations" /> to plug their service-collection extension
/// methods into the inherited tests.
/// </summary>
/// <remarks>
/// <para>
/// The base verifies, for each <see cref="ServiceRegistrationKat" /> row, that the registration delegate
/// produces an entry of the expected lifetime in the <see cref="IServiceCollection" />, and that the
/// service resolves (or does not resolve) from the built provider.
/// </para>
/// </remarks>
public abstract class ServiceLifetimeContractTests
{
    /// <summary>
    /// Returns the service-registration scenarios under test.
    /// </summary>
    protected abstract IReadOnlyList<ServiceRegistrationKat> Registrations { get; }

    /// <summary>
    /// Verifies that every <see cref="ServiceRegistrationKat" /> with <see cref="ServiceRegistrationKat.ShouldResolve" />
    /// set to <see langword="true" /> resolves from a built <see cref="IServiceProvider" />.
    /// </summary>
    [TestMethod]
    public void BuildServiceProvider_WhenServiceRegistered_ShouldResolveService()
    {
        foreach (ServiceRegistrationKat kat in Registrations)
        {
            if (!kat.ShouldResolve)
                continue;

            ServiceCollection services = new();
            kat.Register(services);
            ServiceProvider provider = services.BuildServiceProvider();

            object? resolved = provider.GetService(kat.ServiceType);

            Assert.IsNotNull(resolved, $"KAT '{kat.Name}': {kat.ServiceType} should resolve.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="ServiceRegistrationKat" /> with an explicit
    /// <see cref="ServiceRegistrationKat.ExpectedLifetime" /> registers the service with that lifetime.
    /// </summary>
    [TestMethod]
    public void Register_WhenLifetimeSpecified_ShouldRegisterWithExpectedLifetime()
    {
        foreach (ServiceRegistrationKat kat in Registrations)
        {
            if (kat.ExpectedLifetime is null)
                continue;

            ServiceCollection services = new();
            kat.Register(services);

            ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == kat.ServiceType);

            Assert.IsNotNull(descriptor, $"KAT '{kat.Name}': descriptor for {kat.ServiceType} not found.");
            Assert.AreEqual(kat.ExpectedLifetime.Value, descriptor.Lifetime, $"KAT '{kat.Name}': lifetime mismatch.");
        }
    }
}
