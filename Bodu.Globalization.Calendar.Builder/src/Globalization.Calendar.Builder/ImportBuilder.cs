// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImportBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent surface for configuring an import of an external resource, optionally selecting and re-scoping
/// individual concepts through one or more <c>Use</c> directives.
/// </summary>
/// <remarks>
/// <para>
/// An import that declares no <c>Use</c> directive imports every concept the resource defines; local concepts win over
/// imported ones that share an identifier.
/// </para>
/// </remarks>
public sealed class ImportBuilder
{
    /// <summary>
    /// The selective <c>Use</c> directives, in declaration order.
    /// </summary>
    private readonly List<ImportUseBuilder> _uses = new();

    /// <summary>
    /// The name of the imported resource.
    /// </summary>
    private string _resource;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportBuilder" /> class.
    /// </summary>
    /// <param name="resource">The name of the imported resource.</param>
    internal ImportBuilder(string resource)
    {
        this._resource = resource;
    }

    /// <summary>
    /// Gets the name of the imported resource.
    /// </summary>
    /// <returns>The resource name.</returns>
    internal string Resource =>
        this._resource;

    /// <summary>
    /// Gets the selective <c>Use</c> directives.
    /// </summary>
    /// <returns>The directives; empty when the import imports every concept.</returns>
    internal IReadOnlyList<ImportUseBuilder> Uses =>
        this._uses;

    /// <summary>
    /// Selects and re-scopes a single imported concept through the supplied delegate.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the imported concept.</param>
    /// <param name="configure">A delegate that configures the import-use directive, or <see langword="null" />.</param>
    /// <returns>The same <see cref="ImportBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="notableDateRef" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public ImportBuilder Use(string notableDateRef, Action<ImportUseBuilder>? configure = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(notableDateRef);

        ImportUseBuilder use = new(notableDateRef);
        configure?.Invoke(use);
        this._uses.Add(use);
        return this;
    }

    /// <summary>
    /// Adds a pre-built import-use directive when reconstructing an import from a parsed document.
    /// </summary>
    /// <param name="use">The import-use builder to add.</param>
    internal void AddUse(ImportUseBuilder use) =>
        this._uses.Add(use);

    /// <summary>
    /// Creates a deep copy of this import builder.
    /// </summary>
    /// <returns>A new <see cref="ImportBuilder" /> carrying the same configured state.</returns>
    internal ImportBuilder Clone()
    {
        ImportBuilder clone = new(this._resource);
        foreach (ImportUseBuilder use in this._uses)
            clone._uses.Add(use.Clone());

        return clone;
    }
}
