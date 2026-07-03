// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalTests.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class DiscreteIntervalTests
{
    /// <summary>
    /// Verifies that a bounded interval renders in canonical closed-bracket notation regardless of the constructing
    /// shape.
    /// </summary>
    [TestMethod]
    public void ToString_WhenBounded_ShouldRenderClosedBrackets()
    {
        Assert.AreEqual("[1, 10]", DiscreteInterval<int>.Closed(1, 10).ToString());
        Assert.AreEqual("[2, 4]", DiscreteInterval<int>.Open(1, 5).ToString());
        Assert.AreEqual("[7, 7]", DiscreteInterval<int>.Singleton(7).ToString());
    }

    /// <summary>
    /// Verifies that unbounded and half-bounded intervals render with the infinity glyphs and an open bracket on the
    /// unbounded side.
    /// </summary>
    [TestMethod]
    public void ToString_WhenUnbounded_ShouldRenderInfinityGlyphs()
    {
        Assert.AreEqual("[0, +∞)", DiscreteInterval<int>.AtLeast(0).ToString());
        Assert.AreEqual("(-∞, 5]", DiscreteInterval<int>.AtMost(5).ToString());
        Assert.AreEqual("(-∞, +∞)", DiscreteInterval<int>.All.ToString());
    }

    /// <summary>
    /// Verifies that the empty interval renders as the empty-set glyph.
    /// </summary>
    [TestMethod]
    public void ToString_WhenEmpty_ShouldRenderEmptyGlyph()
    {
        Assert.AreEqual("∅", DiscreteInterval<int>.Empty.ToString());
        Assert.AreEqual("∅", DiscreteInterval<int>.Open(3, 4).ToString());
    }
}
