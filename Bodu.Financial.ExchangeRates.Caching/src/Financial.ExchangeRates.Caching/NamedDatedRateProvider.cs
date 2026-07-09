// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NamedDatedRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Pairs an <see cref="IDatedRateProvider" /> with the name it is referenced by inside an
/// <see cref="AggregatingRateProvider" />.
/// </summary>
/// <param name="Name">The name the provider is referenced by in routing and diagnostics.</param>
/// <param name="Provider">The wrapped dated provider.</param>
public readonly record struct NamedDatedRateProvider(string Name, IDatedRateProvider Provider);
