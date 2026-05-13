// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotZero.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfNotZero{T}(T, string)" /> contract across <see cref="int" />,
    /// <see cref="long" />, <see cref="double" />, and <see cref="decimal" />: any non-zero value throws;
    /// zero passes.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    /// <param name="sign">A sentinel: -1 negative, 0 zero, +1 positive.</param>
    [TestMethod]
    [DataRow("int positive → throw", "int", 1)]
    [DataRow("int negative → throw", "int", -1)]
    [DataRow("int zero → pass", "int", 0)]
    [DataRow("long positive → throw", "long", 1)]
    [DataRow("long negative → throw", "long", -1)]
    [DataRow("long zero → pass", "long", 0)]
    [DataRow("double positive → throw", "double", 1)]
    [DataRow("double negative → throw", "double", -1)]
    [DataRow("double zero → pass", "double", 0)]
    [DataRow("decimal positive → throw", "decimal", 1)]
    [DataRow("decimal negative → throw", "decimal", -1)]
    [DataRow("decimal zero → pass", "decimal", 0)]
    public void ThrowIfNotZero_WhenInvokedAcrossNumericTypes_ShouldFollowContract(string testName, string kind, int sign)
    {
        Type? expected = sign != 0 ? typeof(ArgumentOutOfRangeException) : null;
        string? expectedParam = sign != 0 ? "value" : null;

        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfNotZero(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfNotZero((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfNotZero((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfNotZero((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, expected, expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotZero" />, when ValueIsNotZero, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void ThrowIfNotZero_WhenValueIsNotZero_ShouldThrow(int value)
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
