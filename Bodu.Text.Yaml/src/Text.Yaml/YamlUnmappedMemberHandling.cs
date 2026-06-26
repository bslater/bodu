// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlUnmappedMemberHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Specifies how <see cref="YamlSerializer" /> treats a YAML key that does not map to any member of the target type.
/// </summary>
public enum YamlUnmappedMemberHandling
{
    /// <summary>
    /// Silently ignore an unmapped key. This is the default.
    /// </summary>
    Skip = 0,

    /// <summary>
    /// Reject an unmapped key by throwing <see cref="YamlSerializationException" />.
    /// </summary>
    Disallow = 1,
}
