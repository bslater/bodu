// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="HyperLogLog{T}" />, surfacing its precision, register count,
/// standard error, and current cardinality estimate.
/// </summary>
/// <typeparam name="T">Specifies the type of elements counted by the sketch.</typeparam>
internal sealed class HyperLogLogDebugView<T>
    where T : notnull
{
    /// <summary>The <see cref="HyperLogLog{T}" /> instance being debugged.</summary>
    private readonly HyperLogLog<T> _sketch;

    /// <summary>
    /// Initializes a new instance of the <see cref="HyperLogLogDebugView{T}" /> class.
    /// </summary>
    /// <param name="sketch">The sketch instance to inspect. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sketch" /> is <see langword="null" />.</exception>
    public HyperLogLogDebugView(HyperLogLog<T> sketch)
    {
        _sketch = sketch ?? throw new ArgumentNullException(nameof(sketch));
    }

    /// <summary>
    /// Gets the current estimated number of distinct elements.
    /// </summary>
    public double EstimatedCardinality => _sketch.EstimateCardinality();

    /// <summary>
    /// Gets the precision the sketch was constructed with.
    /// </summary>
    public int Precision => _sketch.Precision;

    /// <summary>
    /// Gets the number of registers backing the sketch.
    /// </summary>
    public int RegisterCount => _sketch.RegisterCount;

    /// <summary>
    /// Gets the relative standard error implied by the precision.
    /// </summary>
    public double StandardError => _sketch.StandardError;
}
