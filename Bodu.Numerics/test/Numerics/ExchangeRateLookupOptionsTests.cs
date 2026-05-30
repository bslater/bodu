// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupOptionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Numerics;

[TestClass]
public partial class ExchangeRateLookupOptionsTests
{
    /// <summary>
    /// Verifies that the <see cref="ExchangeRateLookupOptions.Exact" /> factory produces options whose date-resolution
    /// is <see cref="ExchangeRateDateResolution.Exact" /> with zero tolerance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Exact_WhenAccessed_ShouldReturnOptionsWithExactZeroTolerance()
    {
        ExchangeRateLookupOptions options = ExchangeRateLookupOptions.Exact;

        Assert.AreEqual(ExchangeRateDateResolution.Exact, options.DateResolution);
        Assert.AreEqual(0, options.ToleranceDays);
        Assert.IsTrue(options.AllowInverse);
        Assert.IsTrue(options.AllowSameCurrencyIdentityRate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateLookupOptions.PreviousWithin(int)" /> applies the supplied tolerance under
    /// the <see cref="ExchangeRateDateResolution.PreviousOnOrBefore" /> policy.
    /// </summary>
    [TestMethod]
    public void PreviousWithin_WhenCalled_ShouldReturnOptionsWithPreviousPolicyAndTolerance()
    {
        ExchangeRateLookupOptions options = ExchangeRateLookupOptions.PreviousWithin(5);

        Assert.AreEqual(ExchangeRateDateResolution.PreviousOnOrBefore, options.DateResolution);
        Assert.AreEqual(5, options.ToleranceDays);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateLookupOptions.NextWithin(int)" /> applies the supplied tolerance under the
    /// <see cref="ExchangeRateDateResolution.NextOnOrAfter" /> policy.
    /// </summary>
    [TestMethod]
    public void NextWithin_WhenCalled_ShouldReturnOptionsWithNextPolicyAndTolerance()
    {
        ExchangeRateLookupOptions options = ExchangeRateLookupOptions.NextWithin(3);

        Assert.AreEqual(ExchangeRateDateResolution.NextOnOrAfter, options.DateResolution);
        Assert.AreEqual(3, options.ToleranceDays);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateLookupOptions.NearestWithin(int)" /> applies
    /// <see cref="ExchangeRateDateResolution.NearestPreferPrevious" /> with the supplied tolerance.
    /// </summary>
    [TestMethod]
    public void NearestWithin_WhenCalled_ShouldReturnOptionsWithNearestPreferPreviousPolicy()
    {
        ExchangeRateLookupOptions options = ExchangeRateLookupOptions.NearestWithin(7);

        Assert.AreEqual(ExchangeRateDateResolution.NearestPreferPrevious, options.DateResolution);
        Assert.AreEqual(7, options.ToleranceDays);
    }

    /// <summary>
    /// Verifies that negative tolerance arguments throw on every factory method that accepts a tolerance value.
    /// </summary>
    [TestMethod]
    public void Factories_WhenToleranceDaysIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => ExchangeRateLookupOptions.PreviousWithin(-1),
            "toleranceDays");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => ExchangeRateLookupOptions.NextWithin(-1),
            "toleranceDays");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => ExchangeRateLookupOptions.NearestWithin(-1),
            "toleranceDays");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateLookupOptions.Validate" /> accepts a well-formed configuration without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Validate_WhenOptionsAreWellFormed_ShouldNotThrow()
    {
        ExchangeRateLookupOptions options = new(ExchangeRateDateResolution.PreviousOnOrBefore, 5);

        options.Validate();
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateLookupOptions.Validate" /> rejects an undefined enum value for
    /// <c>DateResolution</c>.
    /// </summary>
    [TestMethod]
    public void Validate_WhenDateResolutionIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateLookupOptions options = new((ExchangeRateDateResolution)999);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => options.Validate(),
            "DateResolution");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateLookupOptions.Validate" /> rejects a negative tolerance.
    /// </summary>
    [TestMethod]
    public void Validate_WhenToleranceDaysIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateLookupOptions options = new(ExchangeRateDateResolution.PreviousOnOrBefore, -1);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => options.Validate(),
            "ToleranceDays");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateLookupOptions.Validate" /> rejects <see cref="ExchangeRateDateResolution.Exact" />
    /// with a non-zero tolerance.
    /// </summary>
    [TestMethod]
    public void Validate_WhenExactPolicyHasNonZeroTolerance_ShouldThrowArgumentException()
    {
        ExchangeRateLookupOptions options = new(ExchangeRateDateResolution.Exact, 1);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () => options.Validate(),
            "ToleranceDays");
    }
}
