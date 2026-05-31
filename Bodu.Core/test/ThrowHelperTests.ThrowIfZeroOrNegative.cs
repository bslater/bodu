// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfZeroOrNegative.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrNegative{T}(T, string)" /> does not throw — and on
    /// the ParamName-asserting overload reports nothing — for positive values across the <see cref="int" />,
    /// <see cref="long" />, <see cref="double" />, and <see cref="decimal" /> overloads.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    [TestMethod]
    [DataRow("int positive", "int")]
    [DataRow("long positive", "long")]
    [DataRow("double positive", "double")]
    [DataRow("decimal positive", "decimal")]
    public void ThrowIfZeroOrNegative_WhenValueIsPositive_ShouldNotThrowAndReportNothing(string testName, string kind)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfZeroOrNegative(1, "value"),
            "long" => () => ThrowHelper.ThrowIfZeroOrNegative(1L, "value"),
            "double" => () => ThrowHelper.ThrowIfZeroOrNegative(1.0, "value"),
            "decimal" => () => ThrowHelper.ThrowIfZeroOrNegative(1m, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, null, null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrNegative{T}(T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for zero and negative
    /// values across the <see cref="int" />, <see cref="long" />, <see cref="double" />, and
    /// <see cref="decimal" /> overloads.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    /// <param name="sign">A non-positive sentinel: 0 zero, -1 negative.</param>
    [TestMethod]
    [DataRow("int zero", "int", 0)]
    [DataRow("int negative", "int", -1)]
    [DataRow("long zero", "long", 0)]
    [DataRow("long negative", "long", -1)]
    [DataRow("double zero", "double", 0)]
    [DataRow("double negative", "double", -1)]
    [DataRow("decimal zero", "decimal", 0)]
    [DataRow("decimal negative", "decimal", -1)]
    public void ThrowIfZeroOrNegative_WhenValueIsZeroOrNegative_ShouldThrowOnValue(string testName, string kind, int sign)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfZeroOrNegative(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfZeroOrNegative((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfZeroOrNegative((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfZeroOrNegative((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, typeof(ArgumentOutOfRangeException), "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrNegative" />, when ValueIsPositive, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(42)]
    [DataRow(int.MaxValue)]
    public void ThrowIfZeroOrNegative_WhenValueIsPositive_ShouldNotThrow(int value) => ThrowHelper.ThrowIfZeroOrNegative(value);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrNegative" />, when ValueIsZeroOrNegative, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void ThrowIfZeroOrNegative_WhenValueIsZeroOrNegative_ShouldThrowExactly(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfZeroOrNegative(value);
        });
    }

}
