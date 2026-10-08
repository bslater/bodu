// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverter{T}.Population.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Serialization.Metadata;

namespace Bodu.Text.Toml.Serialization.Converters;

internal sealed partial class ObjectConverter<T>
{
    /// <summary>
    /// Holds the members read for a <typeparamref name="T" /> table that a populated member holds, until they are bound
    /// to that instance, or to a new one when the member holds none.
    /// </summary>
    private sealed class Population
        : ObjectPopulation
    {
        /// <summary>The metadata of <typeparamref name="T" />.</summary>
        private readonly TypeMetadata _metadata;

        /// <summary>The read member values, indexed by member slot.</summary>
        private readonly object?[] _values;

        /// <summary>Whether each member slot was read from the input.</summary>
        private readonly bool[] _present;

        /// <summary>The entries no member matched, or <see langword="null" />.</summary>
        private readonly Dictionary<string, TomlNode?>? _extensionEntries;

        /// <summary>The serializer options.</summary>
        private readonly TomlSerializerOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="Population" /> class.
        /// </summary>
        /// <param name="metadata">The metadata of <typeparamref name="T" />.</param>
        /// <param name="values">The read member values, indexed by member slot.</param>
        /// <param name="present">Whether each member slot was read from the input.</param>
        /// <param name="extensionEntries">The entries no member matched, or <see langword="null" />.</param>
        /// <param name="options">The serializer options.</param>
        internal Population(
            TypeMetadata metadata,
            object?[] values,
            bool[] present,
            Dictionary<string, TomlNode?>? extensionEntries,
            TomlSerializerOptions options)
        {
            _metadata = metadata;
            _values = values;
            _present = present;
            _extensionEntries = extensionEntries;
            _options = options;
        }

        /// <inheritdoc />
        internal override object Create()
        {
            object instance = ObjectBinder.Construct(_metadata, _values, _present);
            Bind(_metadata, _values, _present, _extensionEntries, instance, _options);
            return instance;
        }

        /// <inheritdoc />
        internal override void Populate(object instance) =>
            Bind(_metadata, _values, _present, _extensionEntries, instance, _options);
    }
}
