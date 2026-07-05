// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Table{T,T,T}.Views.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Globalization;

namespace Bodu.Collections.Generic;

public sealed partial class Table<TRow, TColumn, TValue>
{
    /// <summary>
    /// Gets a live view of the table's row keys.
    /// </summary>
    /// <value>A read-only collection over the backing store's row keys, reflecting later mutations.</value>
    /// <remarks>
    /// Because empty rows are never retained, every reported row holds at least one cell. The collection is the backing
    /// dictionary's live key collection, so its order matches the table's (unspecified) row order.
    /// </remarks>
    public IReadOnlyCollection<TRow> RowKeys => _rows.Keys;

    /// <summary>
    /// Gets the distinct column keys currently present in any row.
    /// </summary>
    /// <value>A snapshot of the distinct column keys, deduplicated by <see cref="ColumnComparer" />.</value>
    /// <remarks>
    /// The backing store is row-major with no column index, so each access recomputes the set by walking every cell —
    /// an O(cells) operation returning a snapshot, unlike the live <see cref="RowKeys" />. Columns appear in row-major
    /// encounter order.
    /// </remarks>
    public IReadOnlyCollection<TColumn> ColumnKeys
    {
        get
        {
            var seen = new HashSet<TColumn>(_columnComparer);
            var columns = new List<TColumn>();

            foreach (Dictionary<TColumn, TValue> cells in _rows.Values)
            {
                foreach (TColumn column in cells.Keys)
                {
                    if (seen.Add(column))
                        columns.Add(column);
                }
            }

            return columns;
        }
    }

    /// <summary>
    /// Returns a live read-only dictionary view over the cells of the specified row, keyed by column.
    /// </summary>
    /// <param name="row">The row key to project. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A read-only dictionary from column key to cell value for the row; empty when the row is absent.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="row" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The view holds the table and the row key and resolves the row on every access, so it is fully live: cells added
    /// or removed after the view was created are reflected, a view created before the row exists reports empty and
    /// starts reporting cells once the row appears, and it reverts to empty when the row's last cell is removed. Each
    /// call allocates a new lightweight view instance.
    /// </para>
    /// <para>
    /// Every member is an O(1) delegation to the row's live cell dictionary. Mutating the table while enumerating the
    /// view surfaces the standard <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> fail-fast
    /// behaviour (<see cref="InvalidOperationException" />).
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<TColumn, TValue> Row(TRow row)
    {
        ThrowHelper.ThrowIfNull(row);

        return new RowView(this, row);
    }

    /// <summary>
    /// Returns a live read-only dictionary view over the specified column's cells across all rows, keyed by row.
    /// </summary>
    /// <param name="column">The column key to project. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A read-only dictionary from row key to cell value for the column; empty when no row holds the column.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="column" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The view holds the table and the column key and resolves against the live backing store on every access, so it
    /// reflects all later mutations, including a view created before any row holds the column. Each call allocates a
    /// new lightweight view instance.
    /// </para>
    /// <para>
    /// The backing store is row-major with no column index, so the view's aggregate members are honest about their
    /// cost: <see cref="IReadOnlyCollection{T}.Count" /> and each full enumeration scan every row and are O(rows) per
    /// access, while the per-row lookups (the indexer, <c>ContainsKey</c>, <c>TryGetValue</c>) are O(1). Mutating the
    /// table while enumerating the view surfaces the standard
    /// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> fail-fast behaviour (<see cref="InvalidOperationException" />).
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<TRow, TValue> Column(TColumn column)
    {
        ThrowHelper.ThrowIfNull(column);

        return new ColumnView(this, column);
    }

    /// <summary>
    /// Returns a lazy sequence pairing each row key with a live row view over that row's cells.
    /// </summary>
    /// <returns>
    /// A sequence of <see cref="KeyValuePair{TKey, TValue}" /> entries associating each row key with the same live view
    /// that <see cref="Row(TRow)" /> returns for it.
    /// </returns>
    /// <remarks>
    /// The sequence enumerates the backing store's row keys lazily, so mutating the table while iterating surfaces the
    /// standard <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> fail-fast behaviour (<see cref="InvalidOperationException" />).
    /// Because empty rows are never retained, every yielded view holds at least one cell at the moment it is yielded.
    /// </remarks>
    public IEnumerable<KeyValuePair<TRow, IReadOnlyDictionary<TColumn, TValue>>> RowMap()
    {
        foreach (TRow row in _rows.Keys)
            yield return new KeyValuePair<TRow, IReadOnlyDictionary<TColumn, TValue>>(row, new RowView(this, row));
    }

    /// <summary>
    /// Provides the live read-only row projection returned by <see cref="Row(TRow)" />, resolving the row's cell
    /// dictionary on every access.
    /// </summary>
    private sealed class RowView : IReadOnlyDictionary<TColumn, TValue>
    {
        /// <summary>The table whose backing store the view resolves against.</summary>
        private readonly Table<TRow, TColumn, TValue> _table;

        /// <summary>The row key this view projects.</summary>
        private readonly TRow _row;

        /// <summary>
        /// Initializes a new instance of the <see cref="RowView" /> class for the specified table and row key.
        /// </summary>
        /// <param name="table">The table to project.</param>
        /// <param name="row">The row key to project.</param>
        internal RowView(Table<TRow, TColumn, TValue> table, TRow row)
        {
            _table = table;
            _row = row;
        }

        /// <inheritdoc />
        public int Count => Cells?.Count ?? 0;

        /// <inheritdoc />
        public IEnumerable<TColumn> Keys => Cells?.Keys ?? Enumerable.Empty<TColumn>();

        /// <inheritdoc />
        public IEnumerable<TValue> Values => Cells?.Values ?? Enumerable.Empty<TValue>();

        /// <summary>
        /// Gets the row's live cell dictionary, or <see langword="null" /> when the row is currently absent.
        /// </summary>
        private Dictionary<TColumn, TValue>? Cells =>
            _table._rows.TryGetValue(_row, out Dictionary<TColumn, TValue>? cells) ? cells : null;

        /// <inheritdoc />
        public TValue this[TColumn key]
        {
            get
            {
                ThrowHelper.ThrowIfNull(key);

                return Cells is Dictionary<TColumn, TValue> cells && cells.TryGetValue(key, out TValue? value)
                    ? value
                    : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.KeyNotFound_Dictionary, key));
            }
        }

        /// <inheritdoc />
        public bool ContainsKey(TColumn key)
        {
            ThrowHelper.ThrowIfNull(key);

            return Cells?.ContainsKey(key) == true;
        }

        /// <inheritdoc />
        public bool TryGetValue(TColumn key, out TValue value)
        {
            ThrowHelper.ThrowIfNull(key);

            if (Cells is Dictionary<TColumn, TValue> cells)
                return cells.TryGetValue(key, out value!);

            value = default!;
            return false;
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TColumn, TValue>> GetEnumerator() =>
            (Cells ?? Enumerable.Empty<KeyValuePair<TColumn, TValue>>()).GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Provides the live read-only column projection returned by <see cref="Column(TColumn)" />, scanning the row-major
    /// backing store on every aggregate access.
    /// </summary>
    private sealed class ColumnView : IReadOnlyDictionary<TRow, TValue>
    {
        /// <summary>The table whose backing store the view resolves against.</summary>
        private readonly Table<TRow, TColumn, TValue> _table;

        /// <summary>The column key this view projects.</summary>
        private readonly TColumn _column;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnView" /> class for the specified table and column key.
        /// </summary>
        /// <param name="table">The table to project.</param>
        /// <param name="column">The column key to project.</param>
        internal ColumnView(Table<TRow, TColumn, TValue> table, TColumn column)
        {
            _table = table;
            _column = column;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Counts the rows holding the column by scanning every row — an O(rows) operation per access.
        /// </remarks>
        public int Count
        {
            get
            {
                var count = 0;

                foreach (Dictionary<TColumn, TValue> cells in _table._rows.Values)
                {
                    if (cells.ContainsKey(_column))
                        count++;
                }

                return count;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TRow> Keys => this.Select(static pair => pair.Key);

        /// <inheritdoc />
        public IEnumerable<TValue> Values => this.Select(static pair => pair.Value);

        /// <inheritdoc />
        public TValue this[TRow key]
        {
            get
            {
                ThrowHelper.ThrowIfNull(key);

                return _table._rows.TryGetValue(key, out Dictionary<TColumn, TValue>? cells) && cells.TryGetValue(_column, out TValue? value)
                    ? value
                    : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.KeyNotFound_Dictionary, key));
            }
        }

        /// <inheritdoc />
        public bool ContainsKey(TRow key)
        {
            ThrowHelper.ThrowIfNull(key);

            return _table._rows.TryGetValue(key, out Dictionary<TColumn, TValue>? cells) && cells.ContainsKey(_column);
        }

        /// <inheritdoc />
        public bool TryGetValue(TRow key, out TValue value)
        {
            ThrowHelper.ThrowIfNull(key);

            if (_table._rows.TryGetValue(key, out Dictionary<TColumn, TValue>? cells))
                return cells.TryGetValue(_column, out value!);

            value = default!;
            return false;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Scans every row for the column — an O(rows) operation per full enumeration.
        /// </remarks>
        public IEnumerator<KeyValuePair<TRow, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<TRow, Dictionary<TColumn, TValue>> row in _table._rows)
            {
                if (row.Value.TryGetValue(_column, out TValue? value))
                    yield return new KeyValuePair<TRow, TValue>(row.Key, value);
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
