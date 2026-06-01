// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyRegistrationConflictPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies how <see cref="CurrencyRegistry" /> resolves an attempt to register a custom <see cref="CurrencyInfo" />
/// for an ISO code that already carries a custom registration.
/// </summary>
public enum CurrencyRegistrationConflictPolicy
{
    /// <summary>
    /// Throw an <see cref="System.InvalidOperationException" /> when a custom registration already exists.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// Replace the existing custom registration with the new metadata.
    /// </summary>
    Replace = 1,

    /// <summary>
    /// Leave the existing custom registration in place and ignore the new metadata.
    /// </summary>
    Ignore = 2,
}
