// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImportUseBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent surface for configuring a single imported concept, supplying a local alias, territory scope,
/// category, non-working flag, and the adjustment policies applied to the imported concept.
/// </summary>
public sealed class ImportUseBuilder
{
    /// <summary>
    /// The adjustment policy identifiers applied to the imported concept, in declaration order.
    /// </summary>
    private readonly List<string> _adjustments = new();

    /// <summary>
    /// The identifier of the imported concept.
    /// </summary>
    private string _notableDateRef;

    /// <summary>
    /// The local alias, or <see langword="null" /> when the imported identifier is retained.
    /// </summary>
    private string? _alias;

    /// <summary>
    /// The territory scope, or <see langword="null" /> when unset.
    /// </summary>
    private string? _territory;

    /// <summary>
    /// The category override, or <see langword="null" /> when the imported category is retained.
    /// </summary>
    private NotableDateCategory? _category;

    /// <summary>
    /// The non-working override, or <see langword="null" /> when unset.
    /// </summary>
    private bool? _nonWorking;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportUseBuilder" /> class.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the imported concept.</param>
    internal ImportUseBuilder(string notableDateRef)
    {
        this._notableDateRef = notableDateRef;
    }

    /// <summary>
    /// Gets the identifier of the imported concept.
    /// </summary>
    /// <returns>The imported concept identifier.</returns>
    internal string NotableDateRef =>
        this._notableDateRef;

    /// <summary>
    /// Gets the local alias.
    /// </summary>
    /// <returns>The alias, or <see langword="null" /> when the imported identifier is retained.</returns>
    internal string? Alias =>
        this._alias;

    /// <summary>
    /// Gets the territory scope.
    /// </summary>
    /// <returns>The territory code, or <see langword="null" /> when unset.</returns>
    internal string? Territory =>
        this._territory;

    /// <summary>
    /// Gets the category override.
    /// </summary>
    /// <returns>The category, or <see langword="null" /> when the imported category is retained.</returns>
    internal NotableDateCategory? Category =>
        this._category;

    /// <summary>
    /// Gets the non-working override.
    /// </summary>
    /// <returns>The flag, or <see langword="null" /> when unset.</returns>
    internal bool? NonWorking =>
        this._nonWorking;

    /// <summary>
    /// Gets the adjustment policy identifiers applied to the imported concept.
    /// </summary>
    /// <returns>The policy identifiers; empty when none are configured.</returns>
    internal IReadOnlyList<string> Adjustments =>
        this._adjustments;

    /// <summary>
    /// Sets the local alias of the imported concept.
    /// </summary>
    /// <param name="alias">The local identifier the imported concept is bound to.</param>
    /// <returns>The same <see cref="ImportUseBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="alias" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public ImportUseBuilder As(string alias)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(alias);

        this._alias = alias;
        return this;
    }

    /// <summary>
    /// Sets the territory scope of the imported concept.
    /// </summary>
    /// <param name="code">The ISO territory code.</param>
    /// <returns>The same <see cref="ImportUseBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="code" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public ImportUseBuilder ForTerritory(string code)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(code);

        this._territory = code;
        return this;
    }

    /// <summary>
    /// Sets the category override of the imported concept.
    /// </summary>
    /// <param name="category">The category that overrides the imported category.</param>
    /// <returns>The same <see cref="ImportUseBuilder" /> instance, enabling chained calls.</returns>
    public ImportUseBuilder WithCategory(NotableDateCategory category)
    {
        this._category = category;
        return this;
    }

    /// <summary>
    /// Sets whether the imported concept is a non-working day.
    /// </summary>
    /// <param name="value">The non-working flag.</param>
    /// <returns>The same <see cref="ImportUseBuilder" /> instance, enabling chained calls.</returns>
    public ImportUseBuilder AsNonWorking(bool value = true)
    {
        this._nonWorking = value;
        return this;
    }

    /// <summary>
    /// Adds an adjustment policy reference applied to the imported concept.
    /// </summary>
    /// <param name="policyRef">The identifier of the adjustment policy to apply.</param>
    /// <returns>The same <see cref="ImportUseBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="policyRef" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public ImportUseBuilder WithAdjustment(string policyRef)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(policyRef);

        this._adjustments.Add(policyRef);
        return this;
    }

    /// <summary>
    /// Sets the import-use state directly when reconstructing a builder from a parsed document.
    /// </summary>
    /// <param name="alias">The local alias, or <see langword="null" />.</param>
    /// <param name="territory">The territory scope, or <see langword="null" />.</param>
    /// <param name="category">The category override, or <see langword="null" />.</param>
    /// <param name="nonWorking">The non-working override, or <see langword="null" />.</param>
    /// <param name="adjustments">The adjustment policy references.</param>
    internal void SetParsedValues(string? alias, string? territory, NotableDateCategory? category, bool? nonWorking, IEnumerable<string> adjustments)
    {
        this._alias = alias;
        this._territory = territory;
        this._category = category;
        this._nonWorking = nonWorking;
        this._adjustments.Clear();
        this._adjustments.AddRange(adjustments);
    }

    /// <summary>
    /// Creates a deep copy of this import-use builder.
    /// </summary>
    /// <returns>A new <see cref="ImportUseBuilder" /> carrying the same configured state.</returns>
    internal ImportUseBuilder Clone()
    {
        ImportUseBuilder clone = new(this._notableDateRef);
        clone.SetParsedValues(this._alias, this._territory, this._category, this._nonWorking, this._adjustments);
        return clone;
    }
}
