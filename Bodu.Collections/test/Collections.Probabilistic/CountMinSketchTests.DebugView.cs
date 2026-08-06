// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountMinSketchTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class CountMinSketchTests
{
    /// <summary>
    /// Verifies that <see cref="CountMinSketchDebugView{T}" /> surfaces the sketch's geometry and accumulated
    /// total unchanged.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenSketchPopulated_ShouldSurfaceGeometryAndTotal()
    {
        var sketch = new CountMinSketch<string>(0.01, 0.01);
        sketch.Add("alpha");
        sketch.Add("beta", 4);

        var view = new CountMinSketchDebugView<string>(sketch);

        Assert.AreEqual(sketch.Depth, view.Depth);
        Assert.AreEqual(sketch.Width, view.Width);
        Assert.AreEqual(sketch.Epsilon, view.Epsilon);
        Assert.AreEqual(sketch.Delta, view.Delta);
        Assert.AreEqual(5L, view.TotalCount);
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketchDebugView{T}.TotalCount" /> is zero for an unpopulated sketch.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenSketchEmpty_ShouldReportZeroTotalCount()
    {
        var sketch = new CountMinSketch<string>(0.01, 0.01);

        var view = new CountMinSketchDebugView<string>(sketch);

        Assert.AreEqual(0L, view.TotalCount);
    }

    /// <summary>
    /// Verifies that constructing <see cref="CountMinSketchDebugView{T}" /> with a <see langword="null" />
    /// sketch throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenSketchIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new CountMinSketchDebugView<string>(null!);
        });

        Assert.AreEqual("sketch", ex.ParamName);
    }
}
