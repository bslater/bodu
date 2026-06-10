// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeModels.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Bencode;

/// <summary>
/// A simple file-entry model used to exercise object mapping and canonical key ordering.
/// </summary>
internal sealed class FileEntry
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    /// <returns>The file name.</returns>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file length in bytes.
    /// </summary>
    /// <returns>The file length.</returns>
    public long Length { get; set; }
}

/// <summary>
/// A model with a byte-array member and a list member, used to exercise binary and collection mapping.
/// </summary>
internal sealed class TorrentInfo
{
    /// <summary>
    /// Gets or sets the announce URL.
    /// </summary>
    /// <returns>The announce URL.</returns>
    public string Announce { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the piece hashes.
    /// </summary>
    /// <returns>The piece hashes.</returns>
    public byte[] Pieces { get; set; } = [];

    /// <summary>
    /// Gets or sets the file names.
    /// </summary>
    /// <returns>The file names.</returns>
    public List<string> Files { get; set; } = [];
}

/// <summary>
/// A model with a member of a type Bencode cannot represent, used to verify the unsupported-type behavior.
/// </summary>
internal sealed class FlaggedModel
{
    /// <summary>
    /// Gets or sets a value indicating whether the model is enabled.
    /// </summary>
    /// <returns>Whether the model is enabled.</returns>
    public bool Enabled { get; set; }
}
