// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IQuarterDefinitionProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Verifies the default interface methods of <see cref="IQuarterDefinitionProvider" />, which a provider inherits when
/// it supplies only the quarter-boundary members.
/// </summary>
[TestClass]
public sealed class IQuarterDefinitionProviderTests
{
    /// <summary>
    /// Gets a provider that relies on the default <c>GetFiscalYear</c> implementations. Its quarter grid fixes the
    /// fiscal year to the window 1 December 2023 – 30 November 2024 regardless of the candidate year.
    /// </summary>
    /// <returns>A provider using the interface defaults.</returns>
    private static IQuarterDefinitionProvider Provider =>
        new DateTimeExtensionsTests.ValidQuarterProvider();

    /// <summary>
    /// Verifies that the default <see cref="IQuarterDefinitionProvider.GetFiscalYear(DateTime)" /> returns the candidate
    /// year whose Q1 start and Q4 end bracket the supplied date.
    /// </summary>
    [TestMethod]
    public void GetFiscalYear_WhenDateInRange_ShouldReturnBracketingYear()
    {
        var fiscalYear = Provider.GetFiscalYear(new DateTime(2024, 6, 1));

        Assert.AreEqual(2023, fiscalYear);
    }

    /// <summary>
    /// Verifies that the default <see cref="IQuarterDefinitionProvider.GetFiscalYear(DateTime)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when no candidate year brackets the supplied date.
    /// </summary>
    [TestMethod]
    public void GetFiscalYear_WhenDateOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = Provider.GetFiscalYear(new DateTime(2030, 1, 1)));
    }

    /// <summary>
    /// Verifies that the default <see cref="IQuarterDefinitionProvider.GetFiscalYear(DateOnly)" /> delegates to the
    /// <see cref="DateTime" /> overload.
    /// </summary>
    [TestMethod]
    public void GetFiscalYear_OnDateOnly_ShouldDelegateToDateTimeOverload()
    {
        var fiscalYear = Provider.GetFiscalYear(new DateOnly(2024, 6, 1));

        Assert.AreEqual(2023, fiscalYear);
    }
}
