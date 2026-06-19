// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCodeExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Currencies;

/// <summary>
/// Verifies the lifecycle-status queries exposed by <see cref="CurrencyCodeExtensions" /> over
/// <see cref="CurrencyCode" /> members.
/// </summary>
[TestClass]
public class CurrencyCodeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="CurrencyCodeExtensions.GetStatus" /> reports <see cref="CurrencyStatus.Active" /> for
    /// an active currency.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetStatus_WhenCodeIsActive_ShouldReturnActive()
    {
        Assert.AreEqual(CurrencyStatus.Active, CurrencyCode.USD.GetStatus());
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyCodeExtensions.GetStatus" /> reports <see cref="CurrencyStatus.Historic" />
    /// for a demonetized currency.
    /// </summary>
    [TestMethod]
    public void GetStatus_WhenCodeIsHistoric_ShouldReturnHistoric()
    {
        Assert.AreEqual(CurrencyStatus.Historic, CurrencyCode.DEM.GetStatus());
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyCodeExtensions.GetStatus" /> reports <see cref="CurrencyStatus.None" /> for
    /// the <see cref="CurrencyCode.None" /> sentinel.
    /// </summary>
    [TestMethod]
    public void GetStatus_WhenCodeIsNone_ShouldReturnNone()
    {
        Assert.AreEqual(CurrencyStatus.None, CurrencyCode.None.GetStatus());
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyCodeExtensions.GetStatus" /> reports <see cref="CurrencyStatus.None" /> for an
    /// undefined enum value.
    /// </summary>
    [TestMethod]
    public void GetStatus_WhenCodeIsUndefined_ShouldReturnNone()
    {
        Assert.AreEqual(CurrencyStatus.None, ((CurrencyCode)9999).GetStatus());
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyCodeExtensions.IsHistoric" /> distinguishes historic from active currencies.
    /// </summary>
    [TestMethod]
    public void IsHistoric_ShouldReflectCurrencyLifecycle()
    {
        Assert.IsTrue(CurrencyCode.DEM.IsHistoric());
        Assert.IsFalse(CurrencyCode.USD.IsHistoric());
        Assert.IsFalse(CurrencyCode.None.IsHistoric());
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyCodeExtensions.IsActive" /> distinguishes active from historic currencies.
    /// </summary>
    [TestMethod]
    public void IsActive_ShouldReflectCurrencyLifecycle()
    {
        Assert.IsTrue(CurrencyCode.USD.IsActive());
        Assert.IsFalse(CurrencyCode.DEM.IsActive());
        Assert.IsFalse(CurrencyCode.None.IsActive());
    }

    /// <summary>
    /// Verifies that every <see cref="CurrencyCode" /> member other than <see cref="CurrencyCode.None" /> carries a
    /// concrete lifecycle status, so no currency is left unclassified.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void GetStatus_ForEveryMember_ShouldBeActiveOrHistoricExceptNone()
    {
        foreach (CurrencyCode code in Enum.GetValues<CurrencyCode>())
        {
            CurrencyStatus status = code.GetStatus();

            if (code == CurrencyCode.None)
                Assert.AreEqual(CurrencyStatus.None, status);
            else
                Assert.IsTrue(
                    status is CurrencyStatus.Active or CurrencyStatus.Historic,
                    $"{code} has unexpected status {status}.");
        }
    }
}
