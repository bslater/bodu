// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Bencode;

/// <summary>
/// Configures how values are serialized to and from Bencode, extending <see cref="FormatSerializerOptions" /> with any
/// Bencode-specific settings.
/// </summary>
/// <remarks>
/// Bencode keys are always written in ascending bytewise order, as the grammar requires, regardless of the order in
/// which members are declared; this is not configurable.
/// </remarks>
public sealed class BencodeSerializerOptions
    : FormatSerializerOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializerOptions" /> class with default settings.
    /// </summary>
    public BencodeSerializerOptions()
    {
    }
}
