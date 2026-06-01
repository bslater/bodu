// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnknownCurrencyPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Specifies how <see cref="Money" /> construction treats an ISO 4217-shaped currency code that is not present in
/// <see cref="CurrencyRegistry" />.
/// </summary>
/// <remarks>
/// <para>
/// A code is "unknown" when it passes the structural check (three uppercase ASCII letters) but no shipped or custom
/// <see cref="CurrencyInfo" /> is registered for it. The default behaviour rejects such codes so that a
/// <see cref="Money" /> can never end up storing a sub-minor-unit precision it cannot describe — the failure mode the
/// policy exists to prevent is a value that retains source precision on construction yet reports zero minor units.
/// </para>
/// <para>
/// The non-default members are opt-in escape hatches for staging and import scenarios. They are reached through the
/// explicit factory surface (<see cref="Money.From(decimal, string, UnknownCurrencyPolicy, System.MidpointRounding)" />
/// and <see cref="Money.FromUnchecked(decimal, string, int, System.MidpointRounding)" />); the ordinary constructors
/// always apply <see cref="Reject" />.
/// </para>
/// </remarks>
public enum UnknownCurrencyPolicy
{
    /// <summary>
    /// Reject the code by throwing an <see cref="System.ArgumentException" />. This is the default for all
    /// <see cref="Money" /> constructors.
    /// </summary>
    Reject = 0,

    /// <summary>
    /// Accept the code only when the caller supplies an explicit minor-unit scale, rounding the amount to that scale so
    /// the resulting value's reported minor units stay self-consistent.
    /// </summary>
    AllowWithExplicitScale = 1,

    /// <summary>
    /// Accept the code and store the amount at its source precision without an associated scale. Intended only for
    /// loose import staging; the resulting value reports zero minor units by contract.
    /// </summary>
    AllowUnscaled = 2,
}
