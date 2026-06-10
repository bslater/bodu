// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlObject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Represents a string-keyed table of <see cref="TomlElement" /> values, suitable as the declared type of an
/// extension-data member. It fills the role that <see cref="System.Text.Json.Nodes.JsonObject" /> fills for
/// <c>System.Text.Json</c> extension data, exposing the captured entries through the
/// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" /> surface.
/// </summary>
/// <remarks>
/// A member annotated with <see cref="TomlExtensionDataAttribute" /> may be declared as <see cref="TomlObject" />,
/// <c>IDictionary&lt;string, TomlElement?&gt;</c>, or <c>Dictionary&lt;string, TomlElement?&gt;</c>; the serializer
/// captures unmatched keys into the member and writes its entries back out alongside the type's other members.
/// </remarks>
public sealed class TomlObject
    : Dictionary<string, TomlElement?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlObject" /> class that is empty.
    /// </summary>
    public TomlObject()
        : base(StringComparer.Ordinal)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlObject" /> class containing the supplied entries.
    /// </summary>
    /// <param name="entries">The entries to copy into the new table.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries" /> is <see langword="null" />.
    /// </exception>
    public TomlObject(IEnumerable<KeyValuePair<string, TomlElement?>> entries)
        : base(StringComparer.Ordinal)
    {
        ThrowHelper.ThrowIfNull(entries);

        foreach (KeyValuePair<string, TomlElement?> entry in entries)
            this[entry.Key] = entry.Value;
    }
}
