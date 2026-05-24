// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfZero.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfZero{T}(T, string)" /> contract across <see cref="int" />,
    /// <see cref="long" />, <see cref="double" />, and <see cref="decimal" />: zero throws; non-zero values
    /// (positive and negative) pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    /// <param name="sign">A sentinel: -1 negative, 0 zero, +1 positive.</param>
    [TestMethod]
    [DataRow("int zero → throw", "int", 0)]
    [DataRow("int positive → pass", "int", 1)]
    [DataRow("int negative → pass", "int", -1)]
    [DataRow("long zero → throw", "long", 0)]
    [DataRow("long positive → pass", "long", 1)]
    [DataRow("long negative → pass", "long", -1)]
    [DataRow("double zero → throw", "double", 0)]
    [DataRow("double positive → pass", "double", 1)]
    [DataRow("double negative → pass", "double", -1)]
    [DataRow("decimal zero → throw", "decimal", 0)]
    [DataRow("decimal positive → pass", "decimal", 1)]
    [DataRow("decimal negative → pass", "decimal", -1)]
    public void ThrowIfZero_WhenInvokedAcrossNumericTypes_ShouldFollowContract(string testName, string kind, int sign)
    {
        Type? expected = sign == 0 ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = sign == 0 ? "value" : null;

        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfZero(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfZero((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfZero((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfZero((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, expected, expectedParam);
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
