// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountMinSketchDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="CountMinSketch{T}" />, surfacing its geometry, error parameters,
/// and running total.
/// </summary>
/// <typeparam name="T">Specifies the type of elements counted by the sketch.</typeparam>
internal sealed class CountMinSketchDebugView<T>
    where T : notnull
{
    /// <summary>The <see cref="CountMinSketch{T}" /> instance being debugged.</summary>
    private readonly CountMinSketch<T> _sketch;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountMinSketchDebugView{T}" /> class.
    /// </summary>
    /// <param name="sketch">The sketch instance to inspect. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sketch" /> is <see langword="null" />.</exception>
    public CountMinSketchDebugView(CountMinSketch<T> sketch)
    {
        _sketch = sketch ?? throw new ArgumentNullException(nameof(sketch));
    }

    /// <summary>
    /// Gets the probability that an estimate exceeds the additive error bound.
    /// </summary>
    public double Delta => _sketch.Delta;

    /// <summary>
    /// Gets the number of counter rows in the sketch.
    /// </summary>
    public int Depth => _sketch.Depth;

    /// <summary>
    /// Gets the additive error factor the sketch was sized for.
    /// </summary>
    public double Epsilon => _sketch.Epsilon;

    /// <summary>
    /// Gets the running sum of all count magnitudes added to the sketch.
    /// </summary>
    public long TotalCount => _sketch.TotalCount;

    /// <summary>
    /// Gets the number of counters per row.
    /// </summary>
    public int Width => _sketch.Width;
}
