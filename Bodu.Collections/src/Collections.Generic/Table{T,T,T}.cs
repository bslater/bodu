// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Table{T,T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a two-dimensional map that associates an ordered row/column key pair with a single value, exposing live
/// row and column projections over a row-major backing store.
/// </summary>
/// <typeparam name="TRow">Specifies the type of row keys in the table.</typeparam>
/// <typeparam name="TColumn">Specifies the type of column keys in the table.</typeparam>
/// <typeparam name="TValue">Specifies the type of values stored in the table's cells.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Table{TRow, TColumn, TValue}" /> is the .NET analogue of Guava's <c>Table</c>: a map keyed by two
/// independent keys — a row and a column — whose reason to exist is the <i>projections</i>. A plain
/// <c>Dictionary&lt;(TRow, TColumn), TValue&gt;</c> already covers flat two-key lookup; adopt this type when you also
/// need <see cref="Row(TRow)" /> ("all cells of this row") or <see cref="Column(TColumn)" /> ("this column across all
/// rows") as first-class dictionary views, or the per-row iteration of <see cref="RowMap" />.
/// </para>
/// <para>
/// The backing store is row-major — an outer dictionary from row key to an inner dictionary of that row's cells — so
/// row-oriented operations are O(1) hash lookups. Column-oriented operations have no second index in this version:
/// <see cref="Column(TColumn)" />, <see cref="ContainsColumn(TColumn)" />, and <see cref="RemoveColumn(TColumn)" />
/// scan every row and cost O(rows) per call (per enumeration for the column view), and <see cref="ColumnKeys" /> walks
/// every cell. Choose the row axis for the key you project by most often.
/// </para>
/// <para>
/// The table never retains an empty row: removing a row's last cell — through <see cref="Remove(TRow, TColumn)" /> or
/// <see cref="RemoveColumn(TColumn)" /> — also removes the row itself, so <see cref="ContainsRow(TRow)" /> and
/// <see cref="RowKeys" /> only ever report rows that hold at least one cell.
/// </para>
/// <para>
/// Enumeration yields the flat cells as <see cref="KeyValuePair{TKey, TValue}" /> entries keyed by a
/// <c>(Row, Column)</c> tuple, in row-major order: all cells of one row are contiguous, but the order of rows and the
/// order of cells within a row follow the unspecified, insertion-biased order of
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> — do not rely on it. The views delegate to the
/// live backing dictionaries, so mutating the table while enumerating the table or one of its views surfaces the
/// standard <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> fail-fast behaviour (<see cref="InvalidOperationException" />).
/// </para>
/// <para>
/// <see cref="Table{TRow, TColumn, TValue}" /> is not thread-safe. Concurrent reads and writes, including through the
/// row and column views, require external synchronization.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var sales = new Table<string, int, decimal>();
/// sales.Add("Widgets", 2024, 1200m);
/// sales.Add("Widgets", 2025, 1350m);
/// sales.Add("Gadgets", 2025, 800m);
///
/// // The projections are the point of the type.
/// IReadOnlyDictionary<int, decimal> widgets = sales.Row("Widgets");     // 2024 -> 1200, 2025 -> 1350
/// IReadOnlyDictionary<string, decimal> in2025 = sales.Column(2025);     // Widgets -> 1350, Gadgets -> 800
///
/// sales["Gadgets", 2025] = 850m;             // upsert through the flat indexer…
/// decimal revised = in2025["Gadgets"];       // 850 — the held view is live
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("Count = {Count}, Rows = {_rows.Count}")]
[DebuggerTypeProxy(typeof(TableDebugView<,,>))]
public sealed partial class Table<TRow, TColumn, TValue> : IEnumerable<KeyValuePair<(TRow Row, TColumn Column), TValue>>
    where TRow : notnull
    where TColumn : notnull
{
    /// <summary>The row-major backing store mapping each row key to that row's cells; empty rows are never retained.</summary>
    private readonly Dictionary<TRow, Dictionary<TColumn, TValue>> _rows;

    /// <summary>The comparer applied to column keys in every per-row cell dictionary.</summary>
    private readonly IEqualityComparer<TColumn> _columnComparer;

    /// <summary>The total number of cells across all rows, maintained by every mutation.</summary>
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="Table{TRow, TColumn, TValue}" /> class that is empty and uses the
    /// default row and column comparers.
    /// </summary>
    public Table()
        : this(null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Table{TRow, TColumn, TValue}" /> class that is empty and uses the
    /// specified row and column comparers.
    /// </summary>
    /// <param name="rowComparer">
    /// The equality comparer to use for row keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <param name="columnComparer">
    /// The equality comparer to use for column keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    public Table(IEqualityComparer<TRow>? rowComparer, IEqualityComparer<TColumn>? columnComparer)
    {
        _rows = new Dictionary<TRow, Dictionary<TColumn, TValue>>(rowComparer);
        _columnComparer = columnComparer ?? EqualityComparer<TColumn>.Default;
    }

    /// <summary>
    /// Gets the total number of cells in the table.
    /// </summary>
    /// <value>The number of row/column/value cells across all rows, maintained as an O(1) counter.</value>
    public int Count => _count;

    /// <summary>
    /// Gets a value indicating whether the table contains no cells.
    /// </summary>
    /// <value><see langword="true" /> if the table holds no cells; otherwise, <see langword="false" />.</value>
    public bool IsEmpty => _count == 0;

    /// <summary>
    /// Gets the <see cref="System.Collections.Generic.IEqualityComparer{T}" /> used to determine row key equality.
    /// </summary>
    /// <value>The comparer used for row identity in the row-major backing store.</value>
    public IEqualityComparer<TRow> RowComparer => _rows.Comparer;

    /// <summary>
    /// Gets the <see cref="System.Collections.Generic.IEqualityComparer{T}" /> used to determine column key equality.
    /// </summary>
    /// <value>The comparer used for column identity within every row.</value>
    public IEqualityComparer<TColumn> ColumnComparer => _columnComparer;

    /// <summary>
    /// Gets or sets the value of the cell at the specified row and column.
    /// </summary>
    /// <param name="row">The row key of the cell. Must not be <see langword="null" />.</param>
    /// <param name="column">The column key of the cell. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="row" /> or <paramref name="column" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">The cell does not exist when read.</exception>
    /// <remarks>
    /// The setter upserts: assigning a new row/column pair adds the cell (creating the row when absent), while
    /// assigning an existing pair replaces its value. Use <see cref="Add(TRow, TColumn, TValue)" /> to reject
    /// duplicates instead.
    /// </remarks>
    public TValue this[TRow row, TColumn column]
    {
        get
        {
            ThrowHelper.ThrowIfNull(row);
            ThrowHelper.ThrowIfNull(column);

            return _rows.TryGetValue(row, out Dictionary<TColumn, TValue>? cells) && cells.TryGetValue(column, out TValue? value)
                ? value
                : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.KeyNotFound_Dictionary, (row, column)));
        }

        set
        {
            ThrowHelper.ThrowIfNull(row);
            ThrowHelper.ThrowIfNull(column);

            Dictionary<TColumn, TValue> cells = GetOrAddRow(row);
            if (!cells.ContainsKey(column))
                _count++;

            cells[column] = value;
        }
    }

    /// <summary>
    /// Adds a value at the specified row and column.
    /// </summary>
    /// <param name="row">The row key of the cell to add. Must not be <see langword="null" />.</param>
    /// <param name="column">The column key of the cell to add. Must not be <see langword="null" />.</param>
    /// <param name="value">The value to store in the cell.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="row" /> or <paramref name="column" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The table already contains a cell at the specified row and column.
    /// </exception>
    /// <remarks>
    /// Duplicate cells follow the strict <see cref="System.Collections.Generic.Dictionary{TKey, TValue}.Add(TKey,
    /// TValue)" /> contract and always throw; use the indexer setter to upsert or <see cref="TryAdd(TRow, TColumn,
    /// TValue)" /> to stay non-throwing. Adding to an absent row creates the row.
    /// </remarks>
    public void Add(TRow row, TColumn column, TValue value)
    {
        ThrowHelper.ThrowIfNull(row);
        ThrowHelper.ThrowIfNull(column);

        Dictionary<TColumn, TValue> cells = GetOrAddRow(row);
        if (cells.ContainsKey(column))
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateTableCell);

        cells.Add(column, value);
        _count++;
    }

    /// <summary>
    /// Attempts to add a value at the specified row and column without throwing on a duplicate cell.
    /// </summary>
    /// <param name="row">The row key of the cell to add. Must not be <see langword="null" />.</param>
    /// <param name="column">The column key of the cell to add. Must not be <see langword="null" />.</param>
    /// <param name="value">The value to store in the cell.</param>
    /// <returns>
    /// <see langword="true" /> if the cell was added; <see langword="false" /> if the table already contains a cell at
    /// the specified row and column, in which case the table is unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="row" /> or <paramref name="column" /> is <see langword="null" />.
    /// </exception>
    public bool TryAdd(TRow row, TColumn column, TValue value)
    {
        ThrowHelper.ThrowIfNull(row);
        ThrowHelper.ThrowIfNull(column);

        Dictionary<TColumn, TValue> cells = GetOrAddRow(row);
        if (!cells.TryAdd(column, value))
            return false;

        _count++;
        return true;
    }

    /// <summary>
    /// Attempts to retrieve the value of the cell at the specified row and column.
    /// </summary>
    /// <param name="row">The row key of the cell. Must not be <see langword="null" />.</param>
    /// <param name="column">The column key of the cell. Must not be <see langword="null" />.</param>
    /// <param name="value">
    /// When this method returns, contains the value of the cell, if the cell is found; otherwise, the default value for
    /// the type of the value parameter.
    /// </param>
    /// <returns><see langword="true" /> if the table contains the cell; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="row" /> or <paramref name="column" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetValue(TRow row, TColumn column, out TValue value)
    {
        ThrowHelper.ThrowIfNull(row);
        ThrowHelper.ThrowIfNull(column);

        if (_rows.TryGetValue(row, out Dictionary<TColumn, TValue>? cells))
            return cells.TryGetValue(column, out value!);

        value = default!;
        return false;
    }

    /// <summary>
    /// Determines whether the table contains a cell at the specified row and column.
    /// </summary>
    /// <param name="row">The row key to locate. Must not be <see langword="null" />.</param>
    /// <param name="column">The column key to locate. Must not be <see langword="null" />.</param>
    /// <returns><see langword="true" /> if the cell exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="row" /> or <paramref name="column" /> is <see langword="null" />.
    /// </exception>
    public bool Contains(TRow row, TColumn column)
    {
        ThrowHelper.ThrowIfNull(row);
        ThrowHelper.ThrowIfNull(column);

        return _rows.TryGetValue(row, out Dictionary<TColumn, TValue>? cells) && cells.ContainsKey(column);
    }

    /// <summary>
    /// Determines whether the table contains at least one cell in the specified row.
    /// </summary>
    /// <param name="row">The row key to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the row holds at least one cell; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="row" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Because empty rows are never retained, this is an O(1) hash lookup that is <see langword="true" /> exactly when
    /// the row has one or more cells.
    /// </remarks>
    public bool ContainsRow(TRow row)
    {
        ThrowHelper.ThrowIfNull(row);

        return _rows.ContainsKey(row);
    }

    /// <summary>
    /// Determines whether the table contains at least one cell in the specified column.
    /// </summary>
    /// <param name="column">The column key to locate. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if any row holds a cell in the column; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="column" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// The backing store is row-major with no column index, so this operation scans every row and is O(rows), unlike
    /// the O(1) <see cref="ContainsRow(TRow)" />.
    /// </remarks>
    public bool ContainsColumn(TColumn column)
    {
        ThrowHelper.ThrowIfNull(column);

        foreach (Dictionary<TColumn, TValue> cells in _rows.Values)
        {
            if (cells.ContainsKey(column))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the table contains a cell with the specified value.
    /// </summary>
    /// <param name="value">The value to locate. The value can be <see langword="null" /> for reference types.</param>
    /// <returns>
    /// <see langword="true" /> if any cell holds a value equal to <paramref name="value" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This operation scans every cell and is O(cells). Value equality is determined by
    /// <see cref="EqualityComparer{T}.Default" /> for <typeparamref name="TValue" />.
    /// </remarks>
    public bool ContainsValue(TValue value)
    {
        EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;

        foreach (Dictionary<TColumn, TValue> cells in _rows.Values)
        {
            foreach (TValue cellValue in cells.Values)
            {
                if (comparer.Equals(cellValue, value))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes the cell at the specified row and column.
    /// </summary>
    /// <param name="row">The row key of the cell to remove. Must not be <see langword="null" />.</param>
    /// <param name="column">The column key of the cell to remove. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the cell was found and removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="row" /> or <paramref name="column" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Removing a row's last cell also removes the row itself, so <see cref="ContainsRow(TRow)" /> and
    /// <see cref="RowKeys" /> never report an empty row.
    /// </remarks>
    public bool Remove(TRow row, TColumn column)
    {
        ThrowHelper.ThrowIfNull(row);
        ThrowHelper.ThrowIfNull(column);

        if (_rows.TryGetValue(row, out Dictionary<TColumn, TValue>? cells) && cells.Remove(column))
        {
            _count--;

            // Prune the row on last-cell removal so empty rows never leak into ContainsRow / RowKeys.
            if (cells.Count == 0)
                _rows.Remove(row);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all cells in the specified row.
    /// </summary>
    /// <param name="row">The row key whose cells to remove. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the row existed and its cells were removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="row" /> is <see langword="null" />.</exception>
    public bool RemoveRow(TRow row)
    {
        ThrowHelper.ThrowIfNull(row);

        if (_rows.Remove(row, out Dictionary<TColumn, TValue>? cells))
        {
            _count -= cells.Count;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the specified column's cell from every row.
    /// </summary>
    /// <param name="column">The column key whose cells to remove. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if at least one cell was removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="column" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// The backing store is row-major with no column index, so this operation scans every row and is O(rows). Rows
    /// whose last cell was in the removed column are pruned, so <see cref="ContainsRow(TRow)" /> and
    /// <see cref="RowKeys" /> never report an empty row.
    /// </remarks>
    public bool RemoveColumn(TColumn column)
    {
        ThrowHelper.ThrowIfNull(column);

        List<TRow>? emptiedRows = null;
        var removed = false;

        foreach (KeyValuePair<TRow, Dictionary<TColumn, TValue>> row in _rows)
        {
            if (row.Value.Remove(column))
            {
                removed = true;
                _count--;

                if (row.Value.Count == 0)
                    (emptiedRows ??= []).Add(row.Key);
            }
        }

        // Prune emptied rows after the scan — the outer dictionary cannot be mutated mid-enumeration.
        if (emptiedRows is not null)
        {
            foreach (TRow row in emptiedRows)
                _rows.Remove(row);
        }

        return removed;
    }

    /// <summary>
    /// Removes all cells from the table.
    /// </summary>
    public void Clear()
    {
        _rows.Clear();
        _count = 0;
    }

    /// <summary>
    /// Returns an enumerator that iterates over the table's cells in row-major order.
    /// </summary>
    /// <returns>
    /// An enumerator over the flat cells as <see cref="KeyValuePair{TKey, TValue}" /> entries keyed by a
    /// <c>(Row, Column)</c> tuple.
    /// </returns>
    /// <remarks>
    /// All cells of one row are contiguous, but the order of rows and of cells within a row follows the unspecified,
    /// insertion-biased order of <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> — do not rely on
    /// it. Mutating the table while enumerating surfaces the standard dictionary fail-fast behaviour (<see cref="InvalidOperationException" />).
    /// </remarks>
    public IEnumerator<KeyValuePair<(TRow Row, TColumn Column), TValue>> GetEnumerator()
    {
        foreach (KeyValuePair<TRow, Dictionary<TColumn, TValue>> row in _rows)
        {
            foreach (KeyValuePair<TColumn, TValue> cell in row.Value)
                yield return new KeyValuePair<(TRow Row, TColumn Column), TValue>((row.Key, cell.Key), cell.Value);
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Retrieves the cell dictionary for the specified row, creating and registering it when absent.
    /// </summary>
    /// <param name="row">The row key whose cell dictionary to retrieve.</param>
    /// <returns>The live cell dictionary for the row.</returns>
    private Dictionary<TColumn, TValue> GetOrAddRow(TRow row)
    {
        if (!_rows.TryGetValue(row, out Dictionary<TColumn, TValue>? cells))
        {
            cells = new Dictionary<TColumn, TValue>(_columnComparer);
            _rows.Add(row, cells);
        }

        return cells;
    }
}
