// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryDebugView{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="RangeDictionary{TKey, TValue}" /> contents.
/// </summary>
/// <typeparam name="TKey">The comparable endpoint type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
/// <remarks>
/// This debug view class simplifies inspection by displaying the stored range entries as an array in the debugger.
/// </remarks>
internal sealed class RangeDictionaryDebugView<TKey, TValue>
    where TKey : IComparable<TKey>
{
    /// <summary>The instance of <see cref="RangeDictionary{TKey, TValue}" /> being debugged.</summary>
    private readonly RangeDictionary<TKey, TValue> _rangeDictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeDictionaryDebugView{TKey, TValue}" /> class.
    /// </summary>
    /// <param name="rangeDictionary">The range dictionary instance to debug.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rangeDictionary" /> is <see langword="null" />.
    /// </exception>
    public RangeDictionaryDebugView(RangeDictionary<TKey, TValue> rangeDictionary)
    {
        _rangeDictionary = rangeDictionary ?? throw new ArgumentNullException(nameof(rangeDictionary));
    }

    /// <summary>
    /// Gets the range entries currently contained within the <see cref="RangeDictionary{TKey, TValue}" /> as an array.
    /// </summary>
    /// <remarks>
    /// This property is hidden from the debugger display root to directly expand the entries for quick inspection.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public ValueRange<TKey, TValue>[] Items =>
        _rangeDictionary.ToArray();
}
