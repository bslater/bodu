// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateLookupOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

[TestClass]
public partial class RateLookupOptionsTests
{
    /// <summary>
    /// Verifies that the <see cref="RateLookupOptions.Exact" /> factory produces options whose date-resolution
    /// is <see cref="RateDateResolution.Exact" /> with zero tolerance.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Exact_WhenAccessed_ShouldReturnOptionsWithExactZeroTolerance()
    {
        RateLookupOptions options = RateLookupOptions.Exact;

        Assert.AreEqual(RateDateResolution.Exact, options.DateResolution);
        Assert.AreEqual(0, options.ToleranceDays);
        Assert.IsTrue(options.AllowInverse);
        Assert.IsTrue(options.AllowSameCurrencyIdentityRate);
    }

    /// <summary>
    /// Verifies that <see cref="RateLookupOptions.PreviousWithin(int)" /> applies the supplied tolerance under
    /// the <see cref="RateDateResolution.PreviousOnOrBefore" /> policy.
    /// </summary>
    [TestMethod]
    public void PreviousWithin_WhenCalled_ShouldReturnOptionsWithPreviousPolicyAndTolerance()
    {
        var options = RateLookupOptions.PreviousWithin(5);

        Assert.AreEqual(RateDateResolution.PreviousOnOrBefore, options.DateResolution);
        Assert.AreEqual(5, options.ToleranceDays);
    }

    /// <summary>
    /// Verifies that <see cref="RateLookupOptions.NextWithin(int)" /> applies the supplied tolerance under the
    /// <see cref="RateDateResolution.NextOnOrAfter" /> policy.
    /// </summary>
    [TestMethod]
    public void NextWithin_WhenCalled_ShouldReturnOptionsWithNextPolicyAndTolerance()
    {
        var options = RateLookupOptions.NextWithin(3);

        Assert.AreEqual(RateDateResolution.NextOnOrAfter, options.DateResolution);
        Assert.AreEqual(3, options.ToleranceDays);
    }

    /// <summary>
    /// Verifies that <see cref="RateLookupOptions.NearestWithin(int)" /> applies
    /// <see cref="RateDateResolution.NearestPreferPrevious" /> with the supplied tolerance.
    /// </summary>
    [TestMethod]
    public void NearestWithin_WhenCalled_ShouldReturnOptionsWithNearestPreferPreviousPolicy()
    {
        var options = RateLookupOptions.NearestWithin(7);

        Assert.AreEqual(RateDateResolution.NearestPreferPrevious, options.DateResolution);
        Assert.AreEqual(7, options.ToleranceDays);
    }

    /// <summary>
    /// Verifies that negative tolerance arguments throw on every factory method that accepts a tolerance value.
    /// </summary>
    [TestMethod]
    public void Factories_WhenToleranceDaysIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => RateLookupOptions.PreviousWithin(-1),
            "toleranceDays");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => RateLookupOptions.NextWithin(-1),
            "toleranceDays");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => RateLookupOptions.NearestWithin(-1),
            "toleranceDays");
    }

    /// <summary>
    /// Verifies that <see cref="RateLookupOptions.Validate" /> accepts a well-formed configuration without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Validate_WhenOptionsAreWellFormed_ShouldNotThrow()
    {
        RateLookupOptions options = new(RateDateResolution.PreviousOnOrBefore, 5);

        options.Validate();
    }

    /// <summary>
    /// Verifies that <see cref="RateLookupOptions.Validate" /> rejects an undefined enum value for
    /// <c>DateResolution</c>.
    /// </summary>
    [TestMethod]
    public void Validate_WhenDateResolutionIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        RateLookupOptions options = new((RateDateResolution)999);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            options.Validate,
            "DateResolution");
    }

    /// <summary>
    /// Verifies that <see cref="RateLookupOptions.Validate" /> rejects a negative tolerance.
    /// </summary>
    [TestMethod]
    public void Validate_WhenToleranceDaysIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        RateLookupOptions options = new(RateDateResolution.PreviousOnOrBefore, -1);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            options.Validate,
            "ToleranceDays");
    }

    /// <summary>
    /// Verifies that <see cref="RateLookupOptions.Validate" /> rejects <see cref="RateDateResolution.Exact" />
    /// with a non-zero tolerance.
    /// </summary>
    [TestMethod]
    public void Validate_WhenExactPolicyHasNonZeroTolerance_ShouldThrowArgumentException()
    {
        RateLookupOptions options = new(RateDateResolution.Exact, 1);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            options.Validate,
            "ToleranceDays");
    }
}
