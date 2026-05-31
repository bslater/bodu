// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfPositive.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfPositive{T}(T, string)" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for zero and negative values across the
    /// <see cref="int" />, <see cref="long" />, <see cref="double" />, and <see cref="decimal" /> overloads.
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
    public void ThrowIfPositive_WhenValueIsNonPositive_ShouldNotThrowAndReportNothing(string testName, string kind, int sign)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfPositive(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfPositive((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfPositive((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfPositive((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, null, null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfPositive{T}(T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for positive values
    /// across the <see cref="int" />, <see cref="long" />, <see cref="double" />, and <see cref="decimal" />
    /// overloads.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    [TestMethod]
    [DataRow("int positive", "int")]
    [DataRow("long positive", "long")]
    [DataRow("double positive", "double")]
    [DataRow("decimal positive", "decimal")]
    public void ThrowIfPositive_WhenValueIsPositive_ShouldThrowOnValue(string testName, string kind)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfPositive(1, "value"),
            "long" => () => ThrowHelper.ThrowIfPositive(1L, "value"),
            "double" => () => ThrowHelper.ThrowIfPositive(1.0, "value"),
            "decimal" => () => ThrowHelper.ThrowIfPositive(1m, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, typeof(ArgumentOutOfRangeException), "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfPositive" />, when ValueIsPositive, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(42)]
    [DataRow(int.MaxValue)]
    public void ThrowIfPositive_WhenValueIsPositive_ShouldThrowExactly(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfPositive(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfPositive" />, when ValueIsZeroOrNegative, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void ThrowIfPositive_WhenValueIsZeroOrNegative_ShouldNotThrow(int value) => ThrowHelper.ThrowIfPositive(value);

}
