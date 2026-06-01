// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyRegistryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Verifies the runtime <see cref="CurrencyRegistry" /> — shipped-catalogue lookup, custom registration,
/// enumeration, and thread safety.
/// </summary>
[TestClass]
public partial class CurrencyRegistryTests
{
    /// <summary>
    /// Verifies that the shipped catalogue resolves representative currencies across all three minor-unit
    /// categories.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 2, false)]
    [DataRow("JPY", 0, false)]
    [DataRow("BHD", 3, false)]
    [DataRow("EUR", 2, false)]
    [DataRow("GBP", 2, false)]
    [DataRow("KRW", 0, false)]
    [DataRow("KWD", 3, false)]
    [DataRow("DEM", 2, true)]
    [DataRow("ITL", 0, true)]
    public void TryGet_WhenIsoCodeIsInShippedCatalogue_ShouldReturnExpectedMetadata(string iso, int expectedMinorUnits, bool expectedHistoric)
    {
        var found = CurrencyRegistry.TryGet(iso, out CurrencyInfo? info);

        Assert.IsTrue(found);
        Assert.IsNotNull(info);
        Assert.AreEqual(iso, info!.IsoCode);
        Assert.AreEqual(expectedMinorUnits, info.MinorUnits);
        Assert.AreEqual(expectedHistoric, info.IsHistoric);
    }

    /// <summary>
    /// Verifies that the cash-rounding increment is surfaced for currencies that declare one.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenCurrencyHasCashRoundingIncrement_ShouldSurfaceIt()
    {
        Assert.IsTrue(CurrencyRegistry.TryGet("CHF", out CurrencyInfo? chf));
        Assert.AreEqual(0.05m, chf!.CashRoundingIncrement);

        Assert.IsTrue(CurrencyRegistry.TryGet("SEK", out CurrencyInfo? sek));
        Assert.AreEqual(1m, sek!.CashRoundingIncrement);

        Assert.IsTrue(CurrencyRegistry.TryGet("USD", out CurrencyInfo? usd));
        Assert.AreEqual(0m, usd!.CashRoundingIncrement);
    }

    /// <summary>
    /// Verifies that a historic-currency entry exposes its demonetization date and successor ISO code.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenHistoricCurrency_ShouldExposeDemonetizationDateAndSuccessor()
    {
        Assert.IsTrue(CurrencyRegistry.TryGet("DEM", out CurrencyInfo? dem));

        Assert.IsTrue(dem!.IsHistoric);
        Assert.AreEqual(new DateOnly(2002, 2, 28), dem.DemonetizedOn);
        Assert.AreEqual("EUR", dem.SuccessorIsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.TryGet(string, out CurrencyInfo?)" /> returns
    /// <see langword="false" /> for unknown codes.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenIsoCodeIsUnknown_ShouldReturnFalse()
    {
        var found = CurrencyRegistry.TryGet("ZZZ", out CurrencyInfo? info);

        Assert.IsFalse(found);
        Assert.IsNull(info);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.TryGet(string, out CurrencyInfo?)" /> returns
    /// <see langword="false" /> for a <see langword="null" /> input rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenIsoCodeIsNull_ShouldReturnFalseWithoutThrowing()
    {
        var found = CurrencyRegistry.TryGet(null!, out CurrencyInfo? info);

        Assert.IsFalse(found);
        Assert.IsNull(info);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.Get(string)" /> throws <see cref="KeyNotFoundException" /> for
    /// an unknown ISO code.
    /// </summary>
    [TestMethod]
    public void Get_WhenIsoCodeIsUnknown_ShouldThrowKeyNotFoundException()
    {
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = CurrencyRegistry.Get("ZZZ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.Get(string)" /> throws <see cref="ArgumentNullException" /> for
    /// a <see langword="null" /> ISO code.
    /// </summary>
    [TestMethod]
    public void Get_WhenIsoCodeIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CurrencyRegistry.Get(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.Contains(string)" /> matches the result of <c>TryGet</c>.
    /// </summary>
    [TestMethod]
    public void Contains_WhenIsoCodeKnown_ShouldReturnTrue()
    {
        Assert.IsTrue(CurrencyRegistry.Contains("USD"));
        Assert.IsTrue(CurrencyRegistry.Contains("EUR"));
        Assert.IsFalse(CurrencyRegistry.Contains("ZZZ"));
    }

    /// <summary>
    /// Verifies that the shipped catalogue contains at least 180 currencies, matching the source-generator
    /// output.
    /// </summary>
    [TestMethod]
    public void All_WhenEnumerated_ShouldContainAtLeastOneHundredEightyEntries()
    {
        IReadOnlyCollection<CurrencyInfo> all = CurrencyRegistry.All;

        Assert.IsTrue(all.Count >= 180, $"Expected ≥180 currencies, found {all.Count}.");
    }

    /// <summary>
    /// Verifies that a custom currency can be registered and resolved through subsequent lookups.
    /// </summary>
    [TestMethod]
    public void Register_WhenFreshIsoCode_ShouldAcceptRegistrationAndResolveIt()
    {
        CurrencyInfo custom = new("XQA", 2, 0m, false, null, null);

        CurrencyRegistry.Register(custom);

        Assert.IsTrue(CurrencyRegistry.TryGet("XQA", out CurrencyInfo? resolved));
        Assert.AreEqual(custom, resolved);
    }

    /// <summary>
    /// Verifies that registering the same custom code twice throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenIsoCodeAlreadyRegistered_ShouldThrowInvalidOperationException()
    {
        CurrencyInfo first = new("XQB", 2, 0m, false, null, null);
        CurrencyInfo second = new("XQB", 3, 0m, false, null, null);

        CurrencyRegistry.Register(first);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            CurrencyRegistry.Register(second);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.TryRegister(CurrencyInfo)" /> returns <see langword="false" />
    /// when the code is already registered rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryRegister_WhenIsoCodeAlreadyRegistered_ShouldReturnFalse()
    {
        CurrencyInfo first = new("XQC", 2, 0m, false, null, null);
        CurrencyInfo second = new("XQC", 4, 0m, false, null, null);

        Assert.IsTrue(CurrencyRegistry.TryRegister(first));
        Assert.IsFalse(CurrencyRegistry.TryRegister(second));
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.TryRegister(CurrencyInfo)" /> returns <see langword="false" />
    /// for a <see langword="null" /> input rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryRegister_WhenInfoIsNull_ShouldReturnFalse() => Assert.IsFalse(CurrencyRegistry.TryRegister(null!));

    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.Register(CurrencyInfo)" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> input.
    /// </summary>
    [TestMethod]
    public void Register_WhenInfoIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CurrencyRegistry.Register(null!);
        });
    }
}
