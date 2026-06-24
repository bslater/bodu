// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExtensions.IsPositive.cs" company="Bodu Pty. Ltd.">
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
        /// Gets a value indicating whether this amount is strictly greater than zero.
        /// </summary>
        /// <value><see langword="true" /> when positive; otherwise <see langword="false" />.</value>
        public bool IsPositive =>
            value.Amount > 0m;
    }

#else

    /// <summary>
    /// Returns a value indicating whether the specified amount is strictly greater than zero.
    /// </summary>
    /// <param name="value">The monetary value to test.</param>
    /// <returns><see langword="true" /> when positive; otherwise <see langword="false" />.</returns>
    public static bool IsPositive(this Money value) =>
        value.Amount > 0m;

#endif
}
