// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlReaderNodeKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Classifies a parsed node row in the flat document store as a scalar, a sequence, a mapping, or an unresolved alias.
/// </summary>
internal enum YamlReaderNodeKind
{
    /// <summary>
    /// A scalar node. The resolved value kind is recorded separately on the row.
    /// </summary>
    Scalar = 0,

    /// <summary>
    /// An ordered sequence node whose children are its elements.
    /// </summary>
    Sequence = 1,

    /// <summary>
    /// An unordered mapping node whose children are its key/value pairs.
    /// </summary>
    Mapping = 2,

    /// <summary>
    /// An alias node that references an anchored node elsewhere in the document. Resolved during composition.
    /// </summary>
    Alias = 3,
}
