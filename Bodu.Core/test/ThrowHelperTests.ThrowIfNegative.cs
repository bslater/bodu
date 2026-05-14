// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNegative.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfNegative{T}(T, string)" /> contract for <see cref="int" />,
    /// <see cref="long" />, <see cref="double" />, and <see cref="decimal" />: negative values throw
    /// <see cref="ArgumentOutOfRangeException" /> with the expected ParamName; zero and positive values pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    /// <param name="sign">A sentinel: -1 negative, 0 zero, +1 positive.</param>
    [TestMethod]
    [DataRow("int negative → throw", "int", -1)]
    [DataRow("int zero → pass", "int", 0)]
    [DataRow("int positive → pass", "int", 1)]
    [DataRow("long negative → throw", "long", -1)]
    [DataRow("long zero → pass", "long", 0)]
    [DataRow("long positive → pass", "long", 1)]
    [DataRow("double negative → throw", "double", -1)]
    [DataRow("double zero → pass", "double", 0)]
    [DataRow("double positive → pass", "double", 1)]
    [DataRow("decimal negative → throw", "decimal", -1)]
    [DataRow("decimal zero → pass", "decimal", 0)]
    [DataRow("decimal positive → pass", "decimal", 1)]
    public void ThrowIfNegative_WhenInvokedAcrossNumericTypes_ShouldFollowContract(string testName, string kind, int sign)
    {
        Type? expected = sign < 0 ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = sign < 0 ? "value" : null;

        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfNegative(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfNegative((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfNegative((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfNegative((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, expected, expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNegative(int)" /> throws an <see cref="ArgumentOutOfRangeException" /> when the
    /// value is negative.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void ThrowIfNegative_WhenValueIsNegative_ShouldThrowArgumentOutOfRangeException(int value)
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
