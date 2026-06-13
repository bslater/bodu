// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExtensions.IsNegative.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

public static partial class MoneyExtensions
{
#if BODU_EXTENSION_MEMBERS

    extension(Money value)
    {
        /// <summary>
        /// Gets a value indicating whether this amount is strictly less than zero.
        /// </summary>
        /// <returns><see langword="true" /> when negative; otherwise <see langword="false" />.</returns>
        public bool IsNegative =>
            value.Amount < 0m;
    }

#else

    /// <summary>
    /// Returns a value indicating whether the specified amount is strictly less than zero.
    /// </summary>
    /// <param name="value">The monetary value to test.</param>
    /// <returns><see langword="true" /> when negative; otherwise <see langword="false" />.</returns>
    public static bool IsNegative(this Money value) =>
        value.Amount < 0m;

#endif
}
