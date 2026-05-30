// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TestCurrencies.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Provides static <see cref="ICurrency" /> markers used by the <see cref="Money{TCurrency}" /> and money-conversion
/// tests. Each marker is a sealed empty type that binds a fixed ISO code and decimal-place count to the type system.
/// </summary>
public static class TestCurrencies
{
    /// <summary>
    /// Represents United States dollars (<c>USD</c>, two decimal places).
    /// </summary>
    public sealed class Usd : ICurrency
    {
        /// <inheritdoc />
        public static string IsoCode => "USD";

        /// <inheritdoc />
        public static int DecimalPlaces => 2;
    }

    /// <summary>
    /// Represents Australian dollars (<c>AUD</c>, two decimal places).
    /// </summary>
    public sealed class Aud : ICurrency
    {
        /// <inheritdoc />
        public static string IsoCode => "AUD";

        /// <inheritdoc />
        public static int DecimalPlaces => 2;
    }

    /// <summary>
    /// Represents Japanese yen (<c>JPY</c>, zero decimal places).
    /// </summary>
    public sealed class Jpy : ICurrency
    {
        /// <inheritdoc />
        public static string IsoCode => "JPY";

        /// <inheritdoc />
        public static int DecimalPlaces => 0;
    }
}
