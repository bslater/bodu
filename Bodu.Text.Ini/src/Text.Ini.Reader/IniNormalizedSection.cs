// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniNormalizedSection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Represents one section of a normalized INI document: its name and its key/value entries after duplicate resolution.
/// </summary>
internal sealed class IniNormalizedSection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IniNormalizedSection" /> class with the supplied name and no
    /// entries.
    /// </summary>
    /// <param name="name">The section name.</param>
    public IniNormalizedSection(string name)
    {
        Name = name;
        Entries = [];
    }

    /// <summary>
    /// Gets the section name.
    /// </summary>
    /// <value>The name from the section header.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the section's key/value entries in resolved order.
    /// </summary>
    /// <value>The entries, with duplicate keys already resolved by the configured policy.</value>
    public List<KeyValuePair<string, string>> Entries { get; }
}
