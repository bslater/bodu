// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SampleModels.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Serialization.Infrastructure;

/// <summary>
/// A mutable class with a parameterless constructor and settable properties of varied scalar kinds.
/// </summary>
internal sealed class MutablePerson
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <returns>The name.</returns>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the age.
    /// </summary>
    /// <returns>The age.</returns>
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the person is active.
    /// </summary>
    /// <returns>Whether the person is active.</returns>
    public bool Active { get; set; }
}

/// <summary>
/// A positional record bound through its primary constructor.
/// </summary>
/// <param name="Title">The title.</param>
/// <param name="Year">The year.</param>
internal sealed record MovieRecord(string Title, int Year);

/// <summary>
/// A type with an <see langword="init" />-only required member and an optional member.
/// </summary>
internal sealed class RequiredModel
{
    /// <summary>
    /// Gets the identifier.
    /// </summary>
    /// <returns>The identifier.</returns>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the optional label.
    /// </summary>
    /// <returns>The label.</returns>
    public string? Label { get; init; }
}

/// <summary>
/// A type with members that exercise the attributes: an explicit name, an ignored member, and an ordered member.
/// </summary>
internal sealed class AnnotatedModel
{
    /// <summary>
    /// Gets or sets the renamed member.
    /// </summary>
    /// <returns>The renamed member.</returns>
    [FormatPropertyName("display_name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ignored member.
    /// </summary>
    /// <returns>The ignored member.</returns>
    [FormatIgnore]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conditionally ignored member.
    /// </summary>
    /// <returns>The conditionally ignored member.</returns>
    [FormatIgnore(Condition = FormatIgnoreCondition.WhenWritingNull)]
    public string? Note { get; set; }

    /// <summary>
    /// Gets or sets the first-ordered member.
    /// </summary>
    /// <returns>The first-ordered member.</returns>
    [FormatPropertyOrder(1)]
    public int Second { get; set; }

    /// <summary>
    /// Gets or sets the zero-ordered member.
    /// </summary>
    /// <returns>The zero-ordered member.</returns>
    [FormatPropertyOrder(0)]
    public int First { get; set; }
}

/// <summary>
/// A type with a member governed by a property-level converter attribute.
/// </summary>
internal sealed class ConverterModel
{
    /// <summary>
    /// Gets or sets the code, converted by <see cref="ReverseStringConverter" />.
    /// </summary>
    /// <returns>The code.</returns>
    [FormatConverter(typeof(ReverseStringConverter))]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// A nested graph: a container holding a child object and collections.
/// </summary>
internal sealed class Catalog
{
    /// <summary>
    /// Gets or sets the owner.
    /// </summary>
    /// <returns>The owner.</returns>
    public MutablePerson Owner { get; set; } = new();

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    /// <returns>The tags.</returns>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the port numbers.
    /// </summary>
    /// <returns>The port numbers.</returns>
    public int[] Ports { get; set; } = [];

    /// <summary>
    /// Gets or sets the metadata map.
    /// </summary>
    /// <returns>The metadata map.</returns>
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.Ordinal);
}
