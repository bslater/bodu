// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotFinite.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfNotFinite(double, string?)" /> contract with explicit
    /// ParamName assertions: NaN and ±Infinity throw <see cref="ArgumentOutOfRangeException" /> on "value";
    /// any finite value passes (including zero, denormals, and the boundary <see cref="double.MaxValue" />
    /// / <see cref="double.MinValue" /> values).
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The <see cref="double" /> value passed to the guard.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("NaN → throw on value", double.NaN, true)]
    [DataRow("+Infinity → throw on value", double.PositiveInfinity, true)]
    [DataRow("-Infinity → throw on value", double.NegativeInfinity, true)]
    [DataRow("zero → pass", 0.0, false)]
    [DataRow("negative finite → pass", -1.5, false)]
    [DataRow("MaxValue → pass", double.MaxValue, false)]
    [DataRow("MinValue → pass", double.MinValue, false)]
    [DataRow("Epsilon → pass", double.Epsilon, false)]
    public void ThrowIfNotFinite_Double_WhenInvokedWithVariousValues_ShouldFollowContract(
        string testName, double value, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotFinite(value, nameof(value)),
            expected,
            expectedParam);
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
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(double, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="double.NaN" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Double_WhenValueIsNaN_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(double.NaN);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(double, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="double.NegativeInfinity" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Double_WhenValueIsNegativeInfinity_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(double.NegativeInfinity);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(double, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="double.PositiveInfinity" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Double_WhenValueIsPositiveInfinity_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(double.PositiveInfinity);
        });
    }

    /// <summary>
    /// Same contract matrix as the <see cref="double" /> overload but for the <see cref="float" /> overload.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The <see cref="float" /> value passed to the guard.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("NaN → throw on value", float.NaN, true)]
    [DataRow("+Infinity → throw on value", float.PositiveInfinity, true)]
    [DataRow("-Infinity → throw on value", float.NegativeInfinity, true)]
    [DataRow("zero → pass", 0.0f, false)]
    [DataRow("negative finite → pass", -1.5f, false)]
    [DataRow("MaxValue → pass", float.MaxValue, false)]
    [DataRow("MinValue → pass", float.MinValue, false)]
    [DataRow("Epsilon → pass", float.Epsilon, false)]
    public void ThrowIfNotFinite_Float_WhenInvokedWithVariousValues_ShouldFollowContract(
        string testName, float value, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotFinite(value, nameof(value)),
            expected,
            expectedParam);
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

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(float, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="float.NaN" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Float_WhenValueIsNaN_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(float.NaN);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(float, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="float.NegativeInfinity" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Float_WhenValueIsNegativeInfinity_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(float.NegativeInfinity);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(float, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is <see cref="float.PositiveInfinity" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotFinite_Float_WhenValueIsPositiveInfinity_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotFinite(float.PositiveInfinity);
        });
    }

}
