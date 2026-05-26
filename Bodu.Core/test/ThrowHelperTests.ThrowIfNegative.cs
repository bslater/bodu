// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNegative.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNegative{T}(T, string)" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for zero and positive values across the
    /// <see cref="int" />, <see cref="long" />, <see cref="double" />, and <see cref="decimal" /> overloads.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    /// <param name="sign">A non-negative sentinel: 0 zero, +1 positive.</param>
    [TestMethod]
    [DataRow("int zero", "int", 0)]
    [DataRow("int positive", "int", 1)]
    [DataRow("long zero", "long", 0)]
    [DataRow("long positive", "long", 1)]
    [DataRow("double zero", "double", 0)]
    [DataRow("double positive", "double", 1)]
    [DataRow("decimal zero", "decimal", 0)]
    [DataRow("decimal positive", "decimal", 1)]
    public void ThrowIfNegative_WhenValueIsNonNegative_ShouldNotThrowAndReportNothing(string testName, string kind, int sign)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfNegative(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfNegative((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfNegative((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfNegative((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, null, null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNegative{T}(T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for negative values
    /// across the <see cref="int" />, <see cref="long" />, <see cref="double" />, and <see cref="decimal" />
    /// overloads.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    [TestMethod]
    [DataRow("int negative", "int")]
    [DataRow("long negative", "long")]
    [DataRow("double negative", "double")]
    [DataRow("decimal negative", "decimal")]
    public void ThrowIfNegative_WhenValueIsNegative_ShouldThrowOnValue(string testName, string kind)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfNegative(-1, "value"),
            "long" => () => ThrowHelper.ThrowIfNegative(-1L, "value"),
            "double" => () => ThrowHelper.ThrowIfNegative(-1.0, "value"),
            "decimal" => () => ThrowHelper.ThrowIfNegative(-1m, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, typeof(ArgumentOutOfRangeException), "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNegative(int)" /> throws an <see cref="ArgumentOutOfRangeException" /> when the
    /// value is negative.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void ThrowIfNegative_WhenValueIsNegative_ShouldThrowExactly(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNegative(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNegative(int)" /> does not throw when the value is zero or positive.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(int.MaxValue)]
    public void ThrowIfNegative_WhenValueIsZeroOrPositive_ShouldNotThrow(int value) => ThrowHelper.ThrowIfNegative(value);

}
