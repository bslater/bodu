// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlConverterCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// A <see cref="YamlConverter" /> collection that honors the read-only state of its owning
/// <see cref="YamlSerializerOptions" />, so that the converter set cannot change after the options instance has been
/// used or frozen.
/// </summary>
/// <remarks>
/// Mirrors the guarded converter list of <see cref="System.Text.Json.JsonSerializerOptions" />: once the owning options
/// instance is read-only, every mutating operation throws <see cref="InvalidOperationException" />, which keeps
/// serializer behavior deterministic when the same instance is shared across threads.
/// </remarks>
internal sealed class YamlConverterCollection : Collection<YamlConverter>
{
    /// <summary>The options instance that governs whether the collection may be mutated.</summary>
    private readonly YamlSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlConverterCollection" /> class bound to its owning options.
    /// </summary>
    /// <param name="options">The options instance that governs whether the collection may be mutated.</param>
    internal YamlConverterCollection(YamlSerializerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Inserts a converter at the specified index after verifying the owning options are mutable.
    /// </summary>
    /// <param name="index">The zero-based index at which the converter is inserted.</param>
    /// <param name="item">The converter to insert.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The owning options instance is read-only.</exception>
    protected override void InsertItem(int index, YamlConverter item)
    {
        _options.VerifyMutable();
        ThrowHelper.ThrowIfNull(item);

        base.InsertItem(index, item);
    }

    /// <summary>
    /// Replaces the converter at the specified index after verifying the owning options are mutable.
    /// </summary>
    /// <param name="index">The zero-based index of the converter to replace.</param>
    /// <param name="item">The replacement converter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The owning options instance is read-only.</exception>
    protected override void SetItem(int index, YamlConverter item)
    {
        _options.VerifyMutable();
        ThrowHelper.ThrowIfNull(item);

        base.SetItem(index, item);
    }

    /// <summary>
    /// Removes the converter at the specified index after verifying the owning options are mutable.
    /// </summary>
    /// <param name="index">The zero-based index of the converter to remove.</param>
    /// <exception cref="InvalidOperationException">The owning options instance is read-only.</exception>
    protected override void RemoveItem(int index)
    {
        _options.VerifyMutable();

        base.RemoveItem(index);
    }

    /// <summary>
    /// Removes all converters after verifying the owning options are mutable.
    /// </summary>
    /// <exception cref="InvalidOperationException">The owning options instance is read-only.</exception>
    protected override void ClearItems()
    {
        _options.VerifyMutable();

        base.ClearItems();
    }
}
