// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfZero.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZero{T}(T, string)" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for non-zero values (positive and negative) across
    /// the <see cref="int" />, <see cref="long" />, <see cref="double" />, and <see cref="decimal" />
    /// overloads.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    /// <param name="sign">A non-zero sentinel: -1 negative, +1 positive.</param>
    [TestMethod]
    [DataRow("int positive", "int", 1)]
    [DataRow("int negative", "int", -1)]
    [DataRow("long positive", "long", 1)]
    [DataRow("long negative", "long", -1)]
    [DataRow("double positive", "double", 1)]
    [DataRow("double negative", "double", -1)]
    [DataRow("decimal positive", "decimal", 1)]
    [DataRow("decimal negative", "decimal", -1)]
    public void ThrowIfZero_WhenValueIsNonZero_ShouldNotThrowAndReportNothing(string testName, string kind, int sign)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfZero(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfZero((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfZero((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfZero((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, null, null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZero{T}(T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for zero across the
    /// <see cref="int" />, <see cref="long" />, <see cref="double" />, and <see cref="decimal" /> overloads.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    [TestMethod]
    [DataRow("int zero", "int")]
    [DataRow("long zero", "long")]
    [DataRow("double zero", "double")]
    [DataRow("decimal zero", "decimal")]
    public void ThrowIfZero_WhenValueIsZero_ShouldThrowOnValue(string testName, string kind)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfZero(0, "value"),
            "long" => () => ThrowHelper.ThrowIfZero(0L, "value"),
            "double" => () => ThrowHelper.ThrowIfZero(0.0, "value"),
            "decimal" => () => ThrowHelper.ThrowIfZero(0m, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, typeof(ArgumentOutOfRangeException), "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZero" />, when ValueIsNonZero, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void ThrowIfZero_WhenValueIsNonZero_ShouldNotThrow(int value) => ThrowHelper.ThrowIfZero(value);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZero" />, when ValueIsZero, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    public void ThrowIfZero_WhenValueIsZero_ShouldThrowExactly(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfZero(value);
        });
    }

}
