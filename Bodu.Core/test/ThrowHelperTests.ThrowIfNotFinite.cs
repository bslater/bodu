// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotFinite.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(double, string?)" /> does not throw — and on
    /// the ParamName-asserting overload reports nothing — for finite <see cref="double" /> values, including
    /// zero, negative, and boundary values.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The <see cref="double" /> value passed to the guard.</param>
    [TestMethod]
    [DataRow("zero", 0.0)]
    [DataRow("negative finite", -1.5)]
    [DataRow("MaxValue", double.MaxValue)]
    [DataRow("MinValue", double.MinValue)]
    [DataRow("Epsilon", double.Epsilon)]
    public void ThrowIfNotFinite_Double_WhenValueIsFinite_ShouldNotThrowAndReportNothing(string testName, double value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNotFinite(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(double, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for non-finite
    /// <see cref="double" /> values.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The <see cref="double" /> value passed to the guard.</param>
    [TestMethod]
    [DataRow("NaN", double.NaN)]
    [DataRow("+Infinity", double.PositiveInfinity)]
    [DataRow("-Infinity", double.NegativeInfinity)]
    public void ThrowIfNotFinite_Double_WhenValueIsNotFinite_ShouldThrowOnValue(string testName, double value) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotFinite(value, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

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
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(float, string?)" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for finite <see cref="float" /> values, including
    /// zero, negative, and boundary values.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The <see cref="float" /> value passed to the guard.</param>
    [TestMethod]
    [DataRow("zero", 0.0f)]
    [DataRow("negative finite", -1.5f)]
    [DataRow("MaxValue", float.MaxValue)]
    [DataRow("MinValue", float.MinValue)]
    [DataRow("Epsilon", float.Epsilon)]
    public void ThrowIfNotFinite_Float_WhenValueIsFinite_ShouldNotThrowAndReportNothing(string testName, float value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNotFinite(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotFinite(float, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for non-finite
    /// <see cref="float" /> values.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The <see cref="float" /> value passed to the guard.</param>
    [TestMethod]
    [DataRow("NaN", float.NaN)]
    [DataRow("+Infinity", float.PositiveInfinity)]
    [DataRow("-Infinity", float.NegativeInfinity)]
    public void ThrowIfNotFinite_Float_WhenValueIsNotFinite_ShouldThrowOnValue(string testName, float value) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotFinite(value, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

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
