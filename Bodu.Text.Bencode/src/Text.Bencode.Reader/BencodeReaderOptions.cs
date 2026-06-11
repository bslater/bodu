// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Reader;

/// <summary>
/// Defines the customizations applied when creating a <see cref="Utf8BencodeReader" />, mirroring the role of
/// <see cref="System.Text.Json.JsonReaderOptions" /> for Bencode.
/// </summary>
/// <remarks>
/// Unlike <see cref="System.Text.Json.JsonReaderOptions" />, Bencode has no comment syntax and no trailing-comma
/// concept, so <see cref="MaxDepth" /> is the only configurable member.
/// </remarks>
public struct BencodeReaderOptions
{
    /// <summary>
    /// Gets or sets the maximum container nesting depth the reader will accept.
    /// </summary>
    /// <value>The maximum container nesting depth; <c>0</c> selects the default of 256.</value>
    /// <returns>The maximum container nesting depth, where <c>0</c> selects the default of 256.</returns>
    public int MaxDepth { get; set; }
}
