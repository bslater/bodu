// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceRegistrationKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a dependency-injection service registration: the registration
/// delegate, the service type the resulting provider must resolve, the expected service lifetime, and a
/// flag indicating whether the service should resolve at all.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Register">The delegate that performs the registration against the <see cref="IServiceCollection" />.</param>
/// <param name="ServiceType">The service type the test will ask the provider to resolve.</param>
/// <param name="ExpectedLifetime">The expected <see cref="ServiceLifetime" />, or <see langword="null" /> when the test does not assert lifetime.</param>
/// <param name="ShouldResolve">Whether <see cref="ServiceType" /> should resolve from the built provider.</param>
public sealed record ServiceRegistrationKat(
    string Name,
    Action<IServiceCollection> Register,
    Type ServiceType,
    ServiceLifetime? ExpectedLifetime,
    bool ShouldResolve) : IKat;
