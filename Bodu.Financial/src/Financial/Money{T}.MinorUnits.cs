// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money{T}.MinorUnits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct Money<TCurrency>
{
    /// <summary>
    /// Creates a <see cref="Money{TCurrency}" /> from an exact integer count of minor units (cents, yen, fils, etc.).
    /// </summary>
    /// <param name="minorUnits">The number of minor units in the major unit's smallest denomination.</param>
    /// <returns>The corresponding monetary amount.</returns>
    /// <remarks>
    /// Ledger storage and wire formats often persist money as integer minor units. <see cref="FromMinorUnits(long)" />
    /// is the canonical bridge from that representation. For example, <c>Money&lt;USD&gt;.FromMinorUnits(1999)</c> is
    /// <c>$19.99</c>; <c>Money&lt;JPY&gt;.FromMinorUnits(1234)</c> is <c>¥1,234</c>;
    /// <c>Money&lt;BHD&gt;.FromMinorUnits(12345)</c> is <c>12.345 BHD</c>.
    /// </remarks>
    public static Money<TCurrency> FromMinorUnits(long minorUnits) =>
        FromNormalizedAmount((decimal)minorUnits / CurrencyMetadata<TCurrency>.Value.MinorUnitFactor);

    /// <summary>
    /// Returns this amount as an integer count of minor units.
    /// </summary>
    /// <returns>The amount expressed in minor units.</returns>
    /// <exception cref="OverflowException">
    /// The scaled value falls outside the range of <see cref="long" />. For practical money amounts (up to trillions of
    /// major units) this is unreachable; the exception covers pathological values built from near-
    /// <see cref="decimal.MaxValue" /> inputs.
    /// </exception>
    public long ToMinorUnits() =>
        decimal.ToInt64(_amount * CurrencyMetadata<TCurrency>.Value.MinorUnitFactor);

    /// <summary>
    /// Attempts to express this amount as an integer count of minor units, returning <see langword="false" /> instead
    /// of throwing when the value would overflow <see cref="long" />.
    /// </summary>
    /// <param name="minorUnits">On success, the amount in minor units; otherwise the default value.</param>
    /// <returns><see langword="true" /> when the conversion succeeded; otherwise <see langword="false" />.</returns>
    public bool TryToMinorUnits(out long minorUnits)
    {
        try
        {
            minorUnits = ToMinorUnits();
            return true;
        }
        catch (OverflowException)
        {
            minorUnits = 0;
            return false;
        }
    }
}
