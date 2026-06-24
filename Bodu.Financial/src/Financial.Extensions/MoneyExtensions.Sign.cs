// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExtensions.Sign.cs" company="Bodu Pty. Ltd.">
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
        /// Gets the sign of this amount.
        /// </summary>
        /// <value><c>-1</c>, <c>0</c>, or <c>1</c>.</value>
        public int Sign =>
            Math.Sign(value.Amount);
    }

#else

    /// <summary>
    /// Returns the sign of the specified amount.
    /// </summary>
    /// <param name="value">The monetary value to test.</param>
    /// <returns><c>-1</c>, <c>0</c>, or <c>1</c>.</returns>
    public static int Sign(this Money value) =>
        Math.Sign(value.Amount);

#endif
}
