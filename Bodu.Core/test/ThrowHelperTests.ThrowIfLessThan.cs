// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThan.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThan" />, Nullable, when ValueIsGreaterThanOrEqualToMin, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(5, 5, false)]
    [DataRow(6, 5, false)]
    public void ThrowIfLessThan_Nullable_WhenValueIsGreaterThanOrEqualToMin_ShouldNotThrow(int? value, int min, bool throwIfNull) => ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThan" />, Nullable, when ValueIsLessThanMin, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(2, 5, false)]
    [DataRow(-1, 0, false)]
    public void ThrowIfLessThan_Nullable_WhenValueIsLessThanMin_ShouldThrowArgumentOutOfRangeException(int? value, int min, bool throwIfNull)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
        });
    }

    // Nullable overloads

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThan" />, Nullable, when ValueIsNullAndThrowIfNull, throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    [DataRow(null, 5, true)]
    public void ThrowIfLessThan_Nullable_WhenValueIsNullAndThrowIfNull_ShouldThrowArgumentNullException(int? value, int min, bool throwIfNull)
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThan" />, Nullable, when ValueIsNullAndThrowIfNullIsFalse, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(null, 5, false)]
    public void ThrowIfLessThan_Nullable_WhenValueIsNullAndThrowIfNullIsFalse_ShouldNotThrow(int? value, int min, bool throwIfNull) => ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfLessThan{T}(T, T, string)" /> contract matrix with
    /// explicit ParamName assertions: <c>value &lt; min</c> throws <see cref="ArgumentOutOfRangeException" />
    /// with ParamName "value"; <c>value &gt;= min</c> passes.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value compared against <paramref name="min" />.</param>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("equal → pass", 5, 5, false)]
    [DataRow("greater than → pass", 6, 5, false)]
    [DataRow("less than → throw", 4, 5, true)]
    [DataRow("MinValue vs 0 → throw", int.MinValue, 0, true)]
    [DataRow("MaxValue vs MinValue → pass", int.MaxValue, int.MinValue, false)]
    public void ThrowIfLessThan_WhenInvokedWithVariousPairs_ShouldFollowContract(
        string testName, int value, int min, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(testName, () => ThrowHelper.ThrowIfLessThan(value, min, nameof(value)), expected, expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThan" />, when ValueIsGreaterThanOrEqualToMin, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(6, 5)]
    [DataRow(int.MaxValue, int.MinValue)]
    public void ThrowIfLessThan_WhenValueIsGreaterThanOrEqualToMin_ShouldNotThrow(int value, int min) => ThrowHelper.ThrowIfLessThan(value, min);

    // Non-nullable overloads

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThan" />, when ValueIsLessThanMin, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, 1)]
    [DataRow(5, 6)]
    public void ThrowIfLessThan_WhenValueIsLessThanMin_ShouldThrowArgumentOutOfRangeException(int value, int min)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfLessThan(value, min);
        });
    }

}
