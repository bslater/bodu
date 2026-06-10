// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Toml.Syntax;

namespace Bodu.Text.Serialization.Toml;

/// <summary>
/// Configures how values are serialized to and from TOML, extending <see cref="FormatSerializerOptions" /> with the
/// specification version used when parsing.
/// </summary>
public sealed class TomlSerializerOptions
    : FormatSerializerOptions
{
    /// <summary>
    /// The specification version used when parsing.
    /// </summary>
    private TomlSpecVersion _specVersion = TomlSpecVersion.V1_0;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializerOptions" /> class with default settings.
    /// </summary>
    public TomlSerializerOptions()
    {
    }

    /// <summary>
    /// Gets or sets the TOML specification version whose grammar is accepted when deserializing.
    /// </summary>
    /// <value>The specification version; <see cref="TomlSpecVersion.V1_0" /> by default.</value>
    /// <returns>The configured specification version.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public TomlSpecVersion SpecVersion
    {
        get => _specVersion;
        set
        {
            if (IsReadOnly)
                throw new InvalidOperationException(SerializationResourceStrings.Op_Invalid_OptionsReadOnly);

            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
            _specVersion = value;
        }
    }
}
