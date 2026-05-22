// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotEqual.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotEqual{T}" /> does not throw when the two integer values
    /// are equal.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(-5, -5)]
    [DataRow(int.MaxValue, int.MaxValue)]
    public void ThrowIfNotEqual_WhenIntValuesAreEqual_ShouldNotThrow(int value, int other) => ThrowHelper.ThrowIfNotEqual(value, other);
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotEqual{T}" /> throws <see cref="ArgumentException" />
    /// when the two integer values are not equal.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 2)]
    [DataRow(-1, 0)]
    [DataRow(int.MinValue, int.MaxValue)]
    public void ThrowIfNotEqual_WhenIntValuesAreNotEqual_ShouldThrowExactly(int value, int other)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNotEqual(value, other);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotEqual{T}" /> does not throw when two string values
    /// are equal.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotEqual_WhenStringValuesAreEqual_ShouldNotThrow() => ThrowHelper.ThrowIfNotEqual("hello", "hello");

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotEqual{T}" /> throws <see cref="ArgumentException" />
    /// when two string values are not equal.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotEqual_WhenStringValuesAreNotEqual_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNotEqual("hello", "world");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotEqual{T}" /> reports the correct parameter name in the
    /// thrown exception.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotEqual_WhenValuesAreNotEqual_ShouldReportParamName()
    {
        var value = 10;
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNotEqual(value, 42, "value");
        });

        Assert.AreEqual("value", ex.ParamName);
    }

}
