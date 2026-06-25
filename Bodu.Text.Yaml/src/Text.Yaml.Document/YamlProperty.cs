// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Document;

/// <summary>
/// Represents a single key/value pair within a mapping node of a <see cref="YamlDocument" />.
/// </summary>
public readonly struct YamlProperty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlProperty" /> struct.
    /// </summary>
    /// <param name="name">The key text.</param>
    /// <param name="value">The mapped value element.</param>
    internal YamlProperty(string name, YamlElement value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the key of the pair.
    /// </summary>
    /// <value>The decoded key text.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the value of the pair.
    /// </summary>
    /// <value>The mapped value element.</value>
    public YamlElement Value { get; }

    /// <summary>
    /// Returns the key text of the pair.
    /// </summary>
    /// <returns>The key.</returns>
    public override string ToString() => Name;
}
