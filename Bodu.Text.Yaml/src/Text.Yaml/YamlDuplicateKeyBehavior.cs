// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDuplicateKeyBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Specifies how the reader and document layers treat a mapping that declares the same key more than once.
/// </summary>
/// <remarks>
/// The YAML specification requires mapping keys to be unique, and duplicate-key ambiguity is a common source of
/// configuration-parsing defects, so the default is <see cref="Throw" />. The non-throwing modes exist only for
/// interoperability with lenient producers and resolve the ambiguity deterministically across the document object
/// models and the serializer.
/// </remarks>
public enum YamlDuplicateKeyBehavior
{
    /// <summary>
    /// Reject a duplicate key by throwing <see cref="YamlFormatException" />. This is the default and the only
    /// specification-conformant behavior.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// Keep the first occurrence of a duplicated key and discard later occurrences.
    /// </summary>
    UseFirst = 1,

    /// <summary>
    /// Keep the last occurrence of a duplicated key, replacing any earlier occurrence.
    /// </summary>
    UseLast = 2,
}
