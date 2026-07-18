// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSetDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="RangeSet{T}" /> contents.
/// </summary>
/// <typeparam name="T">The comparable endpoint type.</typeparam>
/// <remarks>
/// This debug view class simplifies inspection by displaying the stored ranges as an array in the debugger.
/// </remarks>
internal sealed class RangeSetDebugView<T>
    where T : IComparable<T>
{
    /// <summary>The instance of <see cref="RangeSet{T}" /> being debugged.</summary>
    private readonly RangeSet<T> _rangeSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeSetDebugView{T}" /> class.
    /// </summary>
    /// <param name="rangeSet">The range set instance to debug.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rangeSet" /> is <see langword="null" />.
    /// </exception>
    public RangeSetDebugView(RangeSet<T> rangeSet)
    {
        _rangeSet = rangeSet ?? throw new ArgumentNullException(nameof(rangeSet));
    }

    /// <summary>
    /// Gets the ranges currently contained within the <see cref="RangeSet{T}" /> as an array.
    /// </summary>
    /// <remarks>
    /// This property is hidden from the debugger display root to directly expand the ranges for quick inspection.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public Range<T>[] Items =>
        _rangeSet.ToArray();
}
