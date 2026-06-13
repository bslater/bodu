// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExtensions.Abs.cs" company="Bodu Pty. Ltd.">
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
        /// Gets the absolute value of this amount.
        /// </summary>
        /// <returns>A <see cref="Money" /> with the same ISO code and a non-negative amount.</returns>
        /// <exception cref="InvalidOperationException">
        /// This value is a default-initialised, currency-less <see cref="Money" />.
        /// </exception>
        public Money Abs
        {
            get
            {
                if (!value.HasCurrency)
                {
                    throw new InvalidOperationException(FinancialResourceStrings.Op_Invalid_MoneyRequiresCurrency);
                }

                return value.Amount < 0m ? -value : value;
            }
        }
    }

#else

    /// <summary>
    /// Returns the absolute value of the specified amount.
    /// </summary>
    /// <param name="value">The monetary value whose magnitude is taken.</param>
    /// <returns>A <see cref="Money" /> with the same ISO code and a non-negative amount.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="value" /> is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    public static Money Abs(this Money value)
    {
        if (!value.HasCurrency)
        {
            throw new InvalidOperationException(FinancialResourceStrings.Op_Invalid_MoneyRequiresCurrency);
        }

        return value.Amount < 0m ? -value : value;
    }

#endif
}
