// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfZeroOrNegative.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfZeroOrNegative{T}(T, string)" /> contract across
    /// <see cref="int" />, <see cref="long" />, <see cref="double" />, and <see cref="decimal" />: zero and
    /// negative values throw; positive values pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    /// <param name="sign">A sentinel: -1 negative, 0 zero, +1 positive.</param>
    [TestMethod]
    [DataRow("int negative → throw", "int", -1)]
    [DataRow("int zero → throw", "int", 0)]
    [DataRow("int positive → pass", "int", 1)]
    [DataRow("long negative → throw", "long", -1)]
    [DataRow("long zero → throw", "long", 0)]
    [DataRow("long positive → pass", "long", 1)]
    [DataRow("double negative → throw", "double", -1)]
    [DataRow("double zero → throw", "double", 0)]
    [DataRow("double positive → pass", "double", 1)]
    [DataRow("decimal negative → throw", "decimal", -1)]
    [DataRow("decimal zero → throw", "decimal", 0)]
    [DataRow("decimal positive → pass", "decimal", 1)]
    public void ThrowIfZeroOrNegative_WhenInvokedAcrossNumericTypes_ShouldFollowContract(string testName, string kind, int sign)
    {
        Type? expected = sign <= 0 ? typeof(ArgumentOutOfRangeException) : null;
        string? expectedParam = sign <= 0 ? "value" : null;

        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfZeroOrNegative(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfZeroOrNegative((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfZeroOrNegative((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfZeroOrNegative((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, expected, expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrNegative" />, when ValueIsZeroOrNegative, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void ThrowIfZeroOrNegative_WhenValueIsZeroOrNegative_ShouldThrow(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfZeroOrNegative(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfZeroOrNegative" />, when ValueIsPositive, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(42)]
    [DataRow(int.MaxValue)]
    public void ThrowIfZeroOrNegative_WhenValueIsPositive_ShouldNotThrow(int value) => ThrowHelper.ThrowIfZeroOrNegative(value);
}
