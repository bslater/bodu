// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerOptions.ConverterList.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

public sealed partial class TomlSerializerOptions
{
    /// <summary>
    /// A guarded converter collection backing <see cref="Converters" />: it rejects a <see langword="null" /> entry
    /// and refuses every mutating operation once the owning <see cref="TomlSerializerOptions" /> have become read-only,
    /// while leaving read access available.
    /// </summary>
    private sealed class ConverterList
        : IList<TomlConverter>
    {
        /// <summary>
        /// The underlying converter storage.
        /// </summary>
        private readonly List<TomlConverter> _items = [];

        /// <summary>
        /// The options instance that owns this list, consulted to determine whether mutation is permitted.
        /// </summary>
        private readonly TomlSerializerOptions _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterList" /> class owned by the specified options.
        /// </summary>
        /// <param name="owner">The options instance whose read-only state gates mutation.</param>
        internal ConverterList(TomlSerializerOptions owner) =>
            _owner = owner;

        /// <summary>
        /// Gets the number of converters in the list.
        /// </summary>
        /// <returns>The converter count.</returns>
        public int Count => _items.Count;

        /// <summary>
        /// Gets a value indicating whether the list rejects mutation.
        /// </summary>
        /// <returns><see langword="true" /> once the owning options are read-only; otherwise <see langword="false" />.</returns>
        public bool IsReadOnly => _owner.IsReadOnly;

        /// <summary>
        /// Gets or sets the converter at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the converter.</param>
        /// <returns>The converter at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the assigned value is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the owning options are read-only.</exception>
        public TomlConverter this[int index]
        {
            get => _items[index];
            set
            {
                _owner.VerifyMutable();
                ThrowHelper.ThrowIfNull(value);

                _items[index] = value;
            }
        }

        /// <summary>
        /// Adds a converter to the end of the list.
        /// </summary>
        /// <param name="item">The converter to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the owning options are read-only.</exception>
        public void Add(TomlConverter item)
        {
            _owner.VerifyMutable();
            ThrowHelper.ThrowIfNull(item);

            _items.Add(item);
        }

        /// <summary>
        /// Removes all converters from the list.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the owning options are read-only.</exception>
        public void Clear()
        {
            _owner.VerifyMutable();

            _items.Clear();
        }

        /// <summary>
        /// Determines whether the list contains the specified converter.
        /// </summary>
        /// <param name="item">The converter to locate.</param>
        /// <returns><see langword="true" /> when the converter is present; otherwise <see langword="false" />.</returns>
        public bool Contains(TomlConverter item) =>
            _items.Contains(item);

        /// <summary>
        /// Copies the converters to the specified array starting at the given index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(TomlConverter[] array, int arrayIndex) =>
            _items.CopyTo(array, arrayIndex);

        /// <summary>
        /// Returns an enumerator that iterates the converters in order.
        /// </summary>
        /// <returns>An enumerator over the converters.</returns>
        public IEnumerator<TomlConverter> GetEnumerator() =>
            _items.GetEnumerator();

        /// <summary>
        /// Returns the zero-based index of the specified converter.
        /// </summary>
        /// <param name="item">The converter to locate.</param>
        /// <returns>The index of the converter, or <c>-1</c> when it is not present.</returns>
        public int IndexOf(TomlConverter item) =>
            _items.IndexOf(item);

        /// <summary>
        /// Inserts a converter at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert.</param>
        /// <param name="item">The converter to insert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="item" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the owning options are read-only.</exception>
        public void Insert(int index, TomlConverter item)
        {
            _owner.VerifyMutable();
            ThrowHelper.ThrowIfNull(item);

            _items.Insert(index, item);
        }

        /// <summary>
        /// Removes the first occurrence of the specified converter.
        /// </summary>
        /// <param name="item">The converter to remove.</param>
        /// <returns><see langword="true" /> when a converter was removed; otherwise <see langword="false" />.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the owning options are read-only.</exception>
        public bool Remove(TomlConverter item)
        {
            _owner.VerifyMutable();

            return _items.Remove(item);
        }

        /// <summary>
        /// Removes the converter at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the converter to remove.</param>
        /// <exception cref="InvalidOperationException">Thrown when the owning options are read-only.</exception>
        public void RemoveAt(int index)
        {
            _owner.VerifyMutable();

            _items.RemoveAt(index);
        }

        /// <summary>
        /// Returns an enumerator that iterates the converters in order.
        /// </summary>
        /// <returns>An enumerator over the converters.</returns>
        IEnumerator IEnumerable.GetEnumerator() =>
            _items.GetEnumerator();
    }
}
