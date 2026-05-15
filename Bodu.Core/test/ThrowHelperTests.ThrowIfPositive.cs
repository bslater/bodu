// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfPositive.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfPositive{T}(T, string)" /> contract across <see cref="int" />,
    /// <see cref="long" />, <see cref="double" />, and <see cref="decimal" />: positive values throw with the
    /// expected ParamName; zero and negative values pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    /// <param name="sign">A sentinel: -1 negative, 0 zero, +1 positive.</param>
    [TestMethod]
    [DataRow("int positive → throw", "int", 1)]
    [DataRow("int zero → pass", "int", 0)]
    [DataRow("int negative → pass", "int", -1)]
    [DataRow("long positive → throw", "long", 1)]
    [DataRow("long zero → pass", "long", 0)]
    [DataRow("long negative → pass", "long", -1)]
    [DataRow("double positive → throw", "double", 1)]
    [DataRow("double zero → pass", "double", 0)]
    [DataRow("double negative → pass", "double", -1)]
    [DataRow("decimal positive → throw", "decimal", 1)]
    [DataRow("decimal zero → pass", "decimal", 0)]
    [DataRow("decimal negative → pass", "decimal", -1)]
    public void ThrowIfPositive_WhenInvokedAcrossNumericTypes_ShouldFollowContract(string testName, string kind, int sign)
    {
        Type? expected = sign > 0 ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = sign > 0 ? "value" : null;

        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfPositive(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfPositive((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfPositive((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfPositive((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, expected, expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfPositive" />, when ValueIsPositive, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(42)]
    [DataRow(int.MaxValue)]
    public void ThrowIfPositive_WhenValueIsPositive_ShouldThrow(int value)
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
