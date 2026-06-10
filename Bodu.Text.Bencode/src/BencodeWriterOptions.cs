// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeWriterOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Defines the customizations applied when creating a <see cref="Utf8BencodeWriter" />, mirroring the role of
/// <see cref="System.Text.Json.JsonWriterOptions" /> for Bencode.
/// </summary>
/// <remarks>
/// Bencode output is always canonical — dictionary keys are emitted in ascending bytewise order and there is no
/// insignificant whitespace — so, unlike <see cref="System.Text.Json.JsonWriterOptions" />, there is no indentation or
/// encoder option; <see cref="MaxDepth" /> is the only configurable member.
/// </remarks>
public struct BencodeWriterOptions
{
    /// <summary>
    /// Gets or sets the maximum container nesting depth the writer will permit.
    /// </summary>
    /// <value>The maximum container nesting depth; <c>0</c> selects the default of 256.</value>
    /// <returns>The maximum container nesting depth, where <c>0</c> selects the default of 256.</returns>
    public int MaxDepth { get; set; }
}
