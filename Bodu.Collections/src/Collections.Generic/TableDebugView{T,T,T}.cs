// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableDebugView{T,T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger view for <see cref="Table{TRow, TColumn, TValue}" /> that displays the flat cells directly in
/// the debugger.
/// </summary>
/// <typeparam name="TRow">The type of row keys in the table.</typeparam>
/// <typeparam name="TColumn">The type of column keys in the table.</typeparam>
/// <typeparam name="TValue">The type of values stored in the table's cells.</typeparam>
/// <remarks>
/// This type is intended to be used as a debugger proxy (e.g., via <c>[DebuggerTypeProxy]</c>) so that the table's
/// cells appear as a flat row-major list in the watch/locals window.
/// </remarks>
internal sealed class TableDebugView<TRow, TColumn, TValue>
    where TRow : notnull
    where TColumn : notnull
{
    /// <summary>The table whose cells are surfaced to the debugger.</summary>
    private readonly Table<TRow, TColumn, TValue> _table;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableDebugView{TRow, TColumn, TValue}" /> class for the specified
    /// <paramref name="table" />.
    /// </summary>
    /// <param name="table">The table to expose in the debugger view.</param>
    /// <exception cref="ArgumentNullException"><paramref name="table" /> is <see langword="null" />.</exception>
    public TableDebugView(Table<TRow, TColumn, TValue> table)
    {
        ThrowHelper.ThrowIfNull(table);
        _table = table;
    }

    /// <summary>
    /// Gets the table's cells as an array for display in the debugger.
    /// </summary>
    /// <value>The flat cells keyed by <c>(Row, Column)</c> tuples, in row-major enumeration order.</value>
    /// <remarks>
    /// Marked with <see cref="DebuggerBrowsableAttribute" /> using <see cref="DebuggerBrowsableState.RootHidden" />, so
    /// the elements appear directly under the parent node in the debugger rather than as a nested <c>Items</c>
    /// property.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<(TRow Row, TColumn Column), TValue>[] Items => [.. _table];
}
