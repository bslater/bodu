// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Document;

/// <summary>
/// Represents a single name/value property of a <see cref="DotEnvElement" /> object.
/// </summary>
public readonly struct DotEnvProperty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvProperty" /> struct.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value element.</param>
    internal DotEnvProperty(string name, DotEnvElement value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    /// <value>The key name.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the property value element.
    /// </summary>
    /// <value>The value element.</value>
    public DotEnvElement Value { get; }
}
