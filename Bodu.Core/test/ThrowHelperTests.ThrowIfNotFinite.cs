// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotFinite.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(double, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="double.NaN" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Double_WhenValueIsNaN_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(double.NaN);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(double, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="double.PositiveInfinity" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Double_WhenValueIsPositiveInfinity_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(double.PositiveInfinity);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(double, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="double.NegativeInfinity" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Double_WhenValueIsNegativeInfinity_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(double.NegativeInfinity);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(double, string?)" /> does not throw for finite
    /// <see cref="double" /> values, including zero, negative, and boundary values.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Double_WhenValueIsFinite_ShouldNotThrow()
    {
        ThrowHelper.ThrowIfNotFinite(0.0);
        ThrowHelper.ThrowIfNotFinite(-0.0);
        ThrowHelper.ThrowIfNotFinite(1.0);
        ThrowHelper.ThrowIfNotFinite(-1.5);
        ThrowHelper.ThrowIfNotFinite(double.MaxValue);
        ThrowHelper.ThrowIfNotFinite(double.MinValue);
        ThrowHelper.ThrowIfNotFinite(double.Epsilon);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(float, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="float.NaN" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Float_WhenValueIsNaN_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(float.NaN);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(float, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="float.PositiveInfinity" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Float_WhenValueIsPositiveInfinity_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(float.PositiveInfinity);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(float, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="float.NegativeInfinity" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Float_WhenValueIsNegativeInfinity_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(float.NegativeInfinity);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(float, string?)" /> does not throw for finite
    /// <see cref="float" /> values, including zero, negative, and boundary values.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Float_WhenValueIsFinite_ShouldNotThrow()
    {
        ThrowHelper.ThrowIfNotFinite(0.0f);
        ThrowHelper.ThrowIfNotFinite(-0.0f);
        ThrowHelper.ThrowIfNotFinite(1.0f);
        ThrowHelper.ThrowIfNotFinite(-1.5f);
        ThrowHelper.ThrowIfNotFinite(float.MaxValue);
        ThrowHelper.ThrowIfNotFinite(float.MinValue);
        ThrowHelper.ThrowIfNotFinite(float.Epsilon);
    }
}
