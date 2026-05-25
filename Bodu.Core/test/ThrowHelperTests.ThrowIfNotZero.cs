// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotZero.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotZero{T}(T, string)" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for zero across the <see cref="int" />,
    /// <see cref="long" />, <see cref="double" />, and <see cref="decimal" /> overloads.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    [TestMethod]
    [DataRow("int zero", "int")]
    [DataRow("long zero", "long")]
    [DataRow("double zero", "double")]
    [DataRow("decimal zero", "decimal")]
    public void ThrowIfNotZero_WhenValueIsZero_ShouldNotThrowAndReportNothing(string testName, string kind)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfNotZero(0, "value"),
            "long" => () => ThrowHelper.ThrowIfNotZero(0L, "value"),
            "double" => () => ThrowHelper.ThrowIfNotZero(0.0, "value"),
            "decimal" => () => ThrowHelper.ThrowIfNotZero(0m, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, null, null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotZero{T}(T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for any non-zero value
    /// across the <see cref="int" />, <see cref="long" />, <see cref="double" />, and <see cref="decimal" />
    /// overloads.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    /// <param name="sign">A sentinel: -1 negative, +1 positive.</param>
    [TestMethod]
    [DataRow("int positive", "int", 1)]
    [DataRow("int negative", "int", -1)]
    [DataRow("long positive", "long", 1)]
    [DataRow("long negative", "long", -1)]
    [DataRow("double positive", "double", 1)]
    [DataRow("double negative", "double", -1)]
    [DataRow("decimal positive", "decimal", 1)]
    [DataRow("decimal negative", "decimal", -1)]
    public void ThrowIfNotZero_WhenValueIsNotZero_ShouldThrowOnValue(string testName, string kind, int sign)
    {
        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfNotZero(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfNotZero((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfNotZero((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfNotZero((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, typeof(ArgumentOutOfRangeException), "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotZero" />, when ValueIsNotZero, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void ThrowIfNotZero_WhenValueIsNotZero_ShouldThrowExactly(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotZero(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotZero" />, when ValueIsZero, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    public void ThrowIfNotZero_WhenValueIsZero_ShouldNotThrow(int value) => ThrowHelper.ThrowIfNotZero(value);

}
