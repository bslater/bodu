// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfZeroOrPositive.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrPositive{T}(T, string)" /> does not throw — and on
    /// the ParamName-asserting overload reports nothing — for negative values across the <see cref="int" />,
    /// <see cref="long" />, <see cref="double" />, and <see cref="decimal" /> overloads.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    [TestMethod]
    [DataRow("int negative", "int")]
    [DataRow("long negative", "long")]
    [DataRow("double negative", "double")]
    [DataRow("decimal negative", "decimal")]
    public void ThrowIfZeroOrPositive_WhenValueIsNegative_ShouldNotThrowAndReportNothing(string testName, string kind)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfZeroOrPositive(-1, "value"),
            "long" => () => ThrowHelper.ThrowIfZeroOrPositive(-1L, "value"),
            "double" => () => ThrowHelper.ThrowIfZeroOrPositive(-1.0, "value"),
            "decimal" => () => ThrowHelper.ThrowIfZeroOrPositive(-1m, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, null, null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrPositive{T}(T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for zero and positive
    /// values across the <see cref="int" />, <see cref="long" />, <see cref="double" />, and
    /// <see cref="decimal" /> overloads.
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
    public void ThrowIfZeroOrPositive_WhenValueIsZeroOrPositive_ShouldThrowOnValue(string testName, string kind, int sign)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfZeroOrPositive(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfZeroOrPositive((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfZeroOrPositive((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfZeroOrPositive((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, typeof(ArgumentOutOfRangeException), "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrPositive" />, when ValueIsNegative, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-100)]
    [DataRow(int.MinValue)]
    public void ThrowIfZeroOrPositive_WhenValueIsNegative_ShouldNotThrow(int value) => ThrowHelper.ThrowIfZeroOrPositive(value);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrPositive" />, when ValueIsZeroOrPositive, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(100)]
    [DataRow(int.MaxValue)]
    public void ThrowIfZeroOrPositive_WhenValueIsZeroOrPositive_ShouldThrowExactly(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfZeroOrPositive(value);
        });
    }

}
