// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEqual.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEqual{T}" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when the two integer values are equal.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataRow(-5, -5)]
    [DataRow(int.MaxValue, int.MaxValue)]
    public void ThrowIfEqual_WhenIntValuesAreEqual_ShouldThrowArgumentOutOfRangeException(int value, int other)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfEqual(value, other);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEqual{T}" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when two string values are equal.
    /// </summary>
    [TestMethod]
    public void ThrowIfEqual_WhenStringValuesAreEqual_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfEqual("hello", "hello");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEqual{T}" /> does not throw when the two integer values
    /// are different.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 2)]
    [DataRow(-1, 0)]
    [DataRow(int.MinValue, int.MaxValue)]
    public void ThrowIfEqual_WhenIntValuesAreNotEqual_ShouldNotThrow(int value, int other)
    {
        ThrowHelper.ThrowIfEqual(value, other);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEqual{T}" /> does not throw when two string values
    /// are different.
    /// </summary>
    [TestMethod]
    public void ThrowIfEqual_WhenStringValuesAreNotEqual_ShouldNotThrow()
    {
        ThrowHelper.ThrowIfEqual("hello", "world");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEqual{T}" /> reports the correct parameter name in the
    /// thrown exception.
    /// </summary>
    [TestMethod]
    public void ThrowIfEqual_WhenValuesAreEqual_ShouldReportParamName()
    {
        int value = 42;
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfEqual(value, 42, "value");
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
