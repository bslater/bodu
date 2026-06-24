// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyTests.InvalidCurrencyMetadata.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Verifies that constructing a <see cref="Money{TCurrency}" /> over an <see cref="ICurrency" /> implementation
/// that reports an out-of-range <see cref="ICurrency.MinorUnits" /> value fails fast with an
/// <see cref="InvalidOperationException" /> rather than surfacing an indirect failure from
/// <see cref="decimal.Round(decimal, int, MidpointRounding)" />.
/// </summary>
public partial class MoneyOfTCurrencyTests
{
    /// <summary>
    /// A custom currency tag that reports a negative minor-unit precision — invalid.
    /// </summary>
    private sealed class NegativeMinorUnitsCurrency
        : ICurrency
    {
        /// <summary>
        /// Gets a sentinel ISO code for the test-only currency tag.
        /// </summary>
        /// <value>The literal three-letter ISO code <c>"XXA"</c>.</value>
        public static string IsoCode => "XXA";

        /// <summary>
        /// Gets a deliberately-invalid negative minor-unit precision.
        /// </summary>
        /// <value>The value <c>-1</c>.</value>
        public static int MinorUnits => -1;
    }

    /// <summary>
    /// A custom currency tag that reports a minor-unit precision above the 28-digit cap of <see cref="decimal" />.
    /// </summary>
    private sealed class OverflowMinorUnitsCurrency
        : ICurrency
    {
        /// <summary>
        /// Gets a sentinel ISO code for the test-only currency tag.
        /// </summary>
        /// <value>The literal three-letter ISO code <c>"XXB"</c>.</value>
        public static string IsoCode => "XXB";

        /// <summary>
        /// Gets a deliberately-invalid minor-unit precision above the 28-digit decimal cap.
        /// </summary>
        /// <value>The value <c>29</c>.</value>
        public static int MinorUnits => 29;
    }

    /// <summary>
    /// A custom currency tag that reports <see cref="int.MaxValue" /> — pathologically out of range.
    /// </summary>
    private sealed class IntMaxMinorUnitsCurrency
        : ICurrency
    {
        /// <summary>
        /// Gets a sentinel ISO code for the test-only currency tag.
        /// </summary>
        /// <value>The literal three-letter ISO code <c>"XXC"</c>.</value>
        public static string IsoCode => "XXC";

        /// <summary>
        /// Gets <see cref="int.MaxValue" /> as the minor-unit precision — far beyond the decimal cap.
        /// </summary>
        /// <value>The value <see cref="int.MaxValue" />.</value>
        public static int MinorUnits => int.MaxValue;
    }

    /// <summary>
    /// Verifies that constructing a <see cref="Money{TCurrency}" /> over a currency reporting negative
    /// <see cref="ICurrency.MinorUnits" /> throws <see cref="InvalidOperationException" /> rather than letting
    /// the failure surface from <see cref="decimal.Round(decimal, int, MidpointRounding)" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCurrencyMinorUnitsIsNegative_ShouldThrowInvalidOperationException()
    {
        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<NegativeMinorUnitsCurrency>(10m);
        });

        Assert.IsTrue(ex.Message.Contains(typeof(NegativeMinorUnitsCurrency).FullName!, StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("-1", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that constructing a <see cref="Money{TCurrency}" /> over a currency reporting an above-cap
    /// <see cref="ICurrency.MinorUnits" /> throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCurrencyMinorUnitsIsTwentyNine_ShouldThrowInvalidOperationException()
    {
        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<OverflowMinorUnitsCurrency>(10m);
        });

        Assert.IsTrue(ex.Message.Contains(typeof(OverflowMinorUnitsCurrency).FullName!, StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("29", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="int.MaxValue" /> as the minor-unit precision is rejected. This locks the cast
    /// arithmetic in the validator against silent wrap-around.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCurrencyMinorUnitsIsIntMaxValue_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<IntMaxMinorUnitsCurrency>(10m);
        });
    }

    /// <summary>
    /// Verifies that the explicit-rounding constructor performs the same validation as the primary constructor.
    /// </summary>
    [TestMethod]
    public void ConstructorWithRounding_WhenCurrencyMinorUnitsIsNegative_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<NegativeMinorUnitsCurrency>(10m, MidpointRounding.AwayFromZero);
        });
    }

    /// <summary>
    /// Verifies that <c>default(Money&lt;TCurrency&gt;)</c> for a broken-metadata currency does not throw,
    /// because constructing the default value never invokes the validating constructor — the rounding step
    /// (and therefore the validation gate) is skipped.
    /// </summary>
    [TestMethod]
    public void DefaultValue_WhenCurrencyMinorUnitsIsInvalid_ShouldNotThrow()
    {
        Money<NegativeMinorUnitsCurrency> defaultValue = default;

        Assert.AreEqual(0m, defaultValue.Amount);
    }
}
