// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfZeroOrPositive.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfZeroOrPositive{T}(T, string)" /> contract across
    /// <see cref="int" />, <see cref="long" />, <see cref="double" />, and <see cref="decimal" />: zero and
    /// positive values throw; negative values pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="kind">The numeric primitive under test.</param>
    /// <param name="sign">A sentinel: -1 negative, 0 zero, +1 positive.</param>
    [TestMethod]
    [DataRow("int positive → throw", "int", 1)]
    [DataRow("int zero → throw", "int", 0)]
    [DataRow("int negative → pass", "int", -1)]
    [DataRow("long positive → throw", "long", 1)]
    [DataRow("long zero → throw", "long", 0)]
    [DataRow("long negative → pass", "long", -1)]
    [DataRow("double positive → throw", "double", 1)]
    [DataRow("double zero → throw", "double", 0)]
    [DataRow("double negative → pass", "double", -1)]
    [DataRow("decimal positive → throw", "decimal", 1)]
    [DataRow("decimal zero → throw", "decimal", 0)]
    [DataRow("decimal negative → pass", "decimal", -1)]
    public void ThrowIfZeroOrPositive_WhenInvokedAcrossNumericTypes_ShouldFollowContract(string testName, string kind, int sign)
    {
        Type? expected = sign >= 0 ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = sign >= 0 ? "value" : null;

        Action act = kind switch
        {
            "int" => () => ThrowHelper.ThrowIfZeroOrPositive(sign, "value"),
            "long" => () => ThrowHelper.ThrowIfZeroOrPositive((long)sign, "value"),
            "double" => () => ThrowHelper.ThrowIfZeroOrPositive((double)sign, "value"),
            "decimal" => () => ThrowHelper.ThrowIfZeroOrPositive((decimal)sign, "value"),
            _ => () => throw new InvalidOperationException($"Unknown kind '{kind}'."),
        };

        AssertGuard(testName, act, expected, expectedParam);
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
