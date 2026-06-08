// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyRegistryTests.Validation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CurrencyRegistryTests
{
    /// <summary>
    /// Verifies that registering a currency whose minor-unit precision is out of range throws
    /// <see cref="ArgumentOutOfRangeException" /> and does not register the entry.
    /// </summary>
    [TestMethod]
    public void Register_WhenMinorUnitsOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CurrencyRegistry.Register(new CurrencyInfo("XQV", 29, 0m, false, null, null));
        });

        Assert.AreEqual("info", ex.ParamName);
        Assert.IsFalse(CurrencyRegistry.Contains("XQV"));
    }

    /// <summary>
    /// Verifies that registering a currency with a negative cash-rounding increment throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenCashRoundingNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CurrencyRegistry.Register(new CurrencyInfo("XQV", 2, -0.05m, false, null, null));
        });

        Assert.AreEqual("info", ex.ParamName);
    }

    /// <summary>
    /// Verifies that registering a currency whose cash-rounding increment is finer than its declared minor units throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenCashRoundingFinerThanMinorUnits_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CurrencyRegistry.Register(new CurrencyInfo("XQV", 0, 0.05m, false, null, null));
        });

        Assert.AreEqual("info", ex.ParamName);
    }

    /// <summary>
    /// Verifies that registering a currency whose numeric code is out of range throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenNumericCodeOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CurrencyRegistry.Register(new CurrencyInfo("XQV", 2, 0m, false, null, null, "Test", 1000));
        });

        Assert.AreEqual("info", ex.ParamName);
    }

    /// <summary>
    /// Verifies that registering a currency whose ISO code is malformed throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenIsoCodeShapeInvalid_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CurrencyRegistry.Register(new CurrencyInfo("xq", 2, 0m, false, null, null));
        });
    }

    /// <summary>
    /// Verifies that registering a historic currency whose successor ISO code is malformed throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenSuccessorIsoShapeInvalid_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CurrencyRegistry.Register(new CurrencyInfo("XQV", 2, 0m, true, null, "eu"));
        });
    }

    /// <summary>
    /// Verifies that registering a non-historic currency that nonetheless declares a successor throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenHistoricFieldsInconsistent_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CurrencyRegistry.Register(new CurrencyInfo("XQV", 2, 0m, false, null, "EUR"));
        });

        Assert.AreEqual("info", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.TryRegister(CurrencyInfo)" /> returns <see langword="false" /> for
    /// invalid metadata rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryRegister_WhenMetadataInvalid_ShouldReturnFalse()
    {
        Assert.IsFalse(CurrencyRegistry.TryRegister(new CurrencyInfo("XQV", 29, 0m, false, null, null)));
        Assert.IsFalse(CurrencyRegistry.Contains("XQV"));
    }

    /// <summary>
    /// Verifies that a consistent historic currency — historic, with a demonetization date and a valid successor —
    /// registers successfully.
    /// </summary>
    [TestMethod]
    public void Register_WhenHistoricMetadataConsistent_ShouldSucceed()
    {
        CurrencyInfo historic = new("XQH", 2, 0m, true, new DateOnly(2001, 1, 1), "EUR");

        CurrencyRegistry.Register(historic);

        Assert.IsTrue(CurrencyRegistry.TryGet("XQH", out CurrencyInfo? resolved));
        Assert.AreEqual(historic, resolved);
    }
}
