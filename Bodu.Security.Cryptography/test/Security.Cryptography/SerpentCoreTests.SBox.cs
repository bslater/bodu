// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.SBox.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Verifies that each S-box circuit substitutes every 4-bit input in every bit position exactly as the S-box table
    /// does.
    /// </summary>
    /// <param name="index">The S-box index.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    public void SBox_WhenGivenBitslicedInputs_ForEachIndex_ShouldMatchTheTable(int index)
    {
        foreach (uint[] input in BitslicedInputs)
        {
            uint x0 = input[0], x1 = input[1], x2 = input[2], x3 = input[3];
            uint e0 = input[0], e1 = input[1], e2 = input[2], e3 = input[3];

            SerpentCore.SBox(index, ref x0, ref x1, ref x2, ref x3);
            SerpentReference.ApplySBox(index, ref e0, ref e1, ref e2, ref e3);

            CollectionAssert.AreEqual(new[] { e0, e1, e2, e3 }, new[] { x0, x1, x2, x3 }, $"S{index} of {input[0]:X8} {input[1]:X8} {input[2]:X8} {input[3]:X8}");
        }
    }

    /// <summary>
    /// Verifies that an S-box index outside 0 to 7 is rejected with <see cref="ArgumentOutOfRangeException" /> naming
    /// the index.
    /// </summary>
    /// <param name="index">The rejected index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(8)]
    public void SBox_WhenIndexIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        uint x0 = 0, x1 = 0, x2 = 0, x3 = 0;

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.SBox(index, ref x0, ref x1, ref x2, ref x3);
        });

        Assert.AreEqual("index", ex.ParamName);
    }

    /// <summary>
    /// Verifies that SBox forbids inlining, so it is compiled on its own rather than into its callers.
    /// </summary>
    /// <remarks>
    /// Under dynamic PGO a caller that inlined it ran out of inlining budget and left the S-box circuits as calls:
    /// Serpent-128 ran at less than half its speed.
    /// </remarks>
    [TestMethod]
    public void SBox_WhenDeclared_ShouldForbidInliningIntoItsCallers()
    {
        MethodInfo method = typeof(SerpentCore).GetMethod("SBox", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsTrue(method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining));
    }
}
