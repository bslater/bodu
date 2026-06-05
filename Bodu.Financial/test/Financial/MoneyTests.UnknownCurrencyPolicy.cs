// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.UnknownCurrencyPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="Money" /> with a structurally valid but unregistered ISO code throws
    /// <see cref="ArgumentException" /> under the default reject policy.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenIsoCodeIsUnregistered_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Money(12.3456m, "XYZ");
        });

        Assert.AreEqual("isoCode", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Money.From(decimal, string, MidpointRounding)" /> rejects an unregistered ISO code with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void From_WhenIsoCodeIsUnregistered_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Money.From(12.3456m, "XYZ");
        });

        Assert.AreEqual("isoCode", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the policy overload rejects an unregistered ISO code under
    /// <see cref="UnknownCurrencyPolicy.Reject" />.
    /// </summary>
    [TestMethod]
    public void From_WhenUnregisteredAndRejectPolicy_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Money.From(12.3456m, "XYZ", UnknownCurrencyPolicy.Reject);
        });

        Assert.AreEqual("isoCode", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="UnknownCurrencyPolicy.AllowWithExplicitScale" /> is rejected by the policy overload
    /// because it requires an explicit scale that only <see cref="Money.FromUnchecked(decimal, string, int, MidpointRounding)" />
    /// can supply.
    /// </summary>
    [TestMethod]
    public void From_WhenUnregisteredAndAllowWithExplicitScalePolicy_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Money.From(12.3456m, "XYZ", UnknownCurrencyPolicy.AllowWithExplicitScale);
        });

        Assert.AreEqual("policy", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="UnknownCurrencyPolicy.AllowUnscaled" /> stores the source precision and reports zero
    /// minor units for an unregistered currency.
    /// </summary>
    [TestMethod]
    public void From_WhenUnregisteredAndAllowUnscaledPolicy_ShouldStoreSourcePrecisionAndReportZeroMinorUnits()
    {
        var money = Money.From(12.3456m, "XYZ", UnknownCurrencyPolicy.AllowUnscaled);

        Assert.AreEqual(12.3456m, money.Amount);
        Assert.AreEqual("XYZ", money.IsoCode);
        Assert.AreEqual(0, money.MinorUnits);
    }

    /// <summary>
    /// Verifies that the policy overload still rounds a registered currency to its registry minor units regardless of
    /// the supplied policy.
    /// </summary>
    [TestMethod]
    public void From_WhenRegisteredAndAllowUnscaledPolicy_ShouldRoundToRegistryMinorUnits()
    {
        var money = Money.From(1.235m, "USD", UnknownCurrencyPolicy.AllowUnscaled);

        Assert.AreEqual(1.24m, money.Amount);
        Assert.AreEqual(2, money.MinorUnits);
    }

    /// <summary>
    /// Verifies that the policy overload throws <see cref="ArgumentOutOfRangeException" /> for an undefined policy
    /// value.
    /// </summary>
    [TestMethod]
    public void From_WhenPolicyIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Money.From(10m, "USD", (UnknownCurrencyPolicy)99);
        });

        Assert.AreEqual("policy", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Money.FromUnchecked(decimal, string, int, MidpointRounding)" /> rounds the amount to
    /// the supplied scale and reports that scale from <see cref="Money.MinorUnits" />.
    /// </summary>
    [TestMethod]
    public void FromUnchecked_WhenExplicitScaleSupplied_ShouldRoundAndReportScale()
    {
        var money = Money.FromUnchecked(1.234567m, "XYZ", 4);

        Assert.AreEqual(1.2346m, money.Amount);
        Assert.AreEqual("XYZ", money.IsoCode);
        Assert.AreEqual(4, money.MinorUnits);
    }

    /// <summary>
    /// Verifies that <see cref="Money.FromUnchecked(decimal, string, int, MidpointRounding)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the minor-unit scale is outside the range 0 to 28.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(29)]
    public void FromUnchecked_WhenScaleOutOfRange_ShouldThrowArgumentOutOfRangeException(int minorUnits)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Money.FromUnchecked(1m, "XYZ", minorUnits);
        });

        Assert.AreEqual("minorUnits", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Money.FromUnchecked(decimal, string, int, MidpointRounding)" /> validates the ISO-code
    /// shape.
    /// </summary>
    [TestMethod]
    public void FromUnchecked_WhenIsoCodeShapeInvalid_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Money.FromUnchecked(1m, "us", 2);
        });
    }

    /// <summary>
    /// Verifies that an explicit-scale value preserves its reported minor units through negation and absolute value.
    /// </summary>
    [TestMethod]
    public void ExplicitScale_WhenNegatedAndAbs_ShouldPreserveMinorUnits()
    {
        var money = Money.FromUnchecked(-1.2300m, "XYZ", 4);

        Assert.AreEqual(4, (-money).MinorUnits);
        Assert.AreEqual(4, money.Abs.MinorUnits);
        Assert.AreEqual(1.23m, money.Abs.Amount);
    }

    /// <summary>
    /// Verifies that the strict parser reports failure (rather than throwing) for a structurally valid but unregistered
    /// ISO code.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenIsoCodeIsUnregistered_ShouldReturnFalse()
    {
        var parsed = Money.TryParse("XYZ 10.00", System.Globalization.CultureInfo.InvariantCulture, out Money result);

        Assert.IsFalse(parsed);
        Assert.AreEqual(default, result);
    }

    /// <summary>
    /// Verifies across a representative spread of registered currencies that construction rounds to the registry
    /// minor-unit precision.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("USD", 2)]
    [DataRow("EUR", 2)]
    [DataRow("GBP", 2)]
    [DataRow("JPY", 0)]
    [DataRow("KRW", 0)]
    [DataRow("CLP", 0)]
    [DataRow("BHD", 3)]
    [DataRow("KWD", 3)]
    [DataRow("OMR", 3)]
    public void Constructor_WhenRegisteredCurrency_ShouldRoundAndReportRegistryMinorUnits(string iso, int expectedMinorUnits)
    {
        Money money = new(1.2345678m, iso);

        Assert.AreEqual(expectedMinorUnits, money.MinorUnits);
        Assert.AreEqual(decimal.Round(1.2345678m, expectedMinorUnits, MidpointRounding.ToEven), money.Amount);
    }
}
