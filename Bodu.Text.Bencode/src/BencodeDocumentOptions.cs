// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Defines the customizations applied when parsing a <see cref="BencodeDocument" />, mirroring the role of
/// <see cref="System.Text.Json.JsonDocumentOptions" /> for Bencode.
/// </summary>
/// <remarks>
/// Unlike <see cref="System.Text.Json.JsonDocumentOptions" />, Bencode has no comment syntax and no trailing-comma
/// concept, so <see cref="MaxDepth" /> is the only configurable member.
/// </remarks>
public struct BencodeDocumentOptions
{
    /// <summary>
    /// Gets or sets the maximum container nesting depth the parser will accept.
    /// </summary>
    /// <value>The maximum container nesting depth; <c>0</c> selects the default of 256.</value>
    /// <returns>The maximum container nesting depth, where <c>0</c> selects the default of 256.</returns>
    public int MaxDepth { get; set; }
}
