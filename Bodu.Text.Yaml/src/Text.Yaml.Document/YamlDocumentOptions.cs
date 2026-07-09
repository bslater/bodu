// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Document;

/// <summary>
/// Provides options that configure how a <see cref="YamlDocument" /> parses YAML source.
/// </summary>
public struct YamlDocumentOptions
{
    /// <summary>The configured maximum nesting depth; zero or less selects the default.</summary>
    private int _maxDepth;

    /// <summary>
    /// Gets or sets the YAML specification version whose implicit type resolution is applied to plain scalars.
    /// </summary>
    /// <value>The specification version; the default is <see cref="YamlSpecVersion.V1_2" />.</value>
    public YamlSpecVersion SpecVersion { get; set; }

    /// <summary>
    /// Gets or sets the policy applied when a mapping declares the same key more than once.
    /// </summary>
    /// <value>The duplicate-key policy; the default is <see cref="YamlDuplicateKeyBehavior.Throw" />.</value>
    public YamlDuplicateKeyBehavior DuplicateKeyBehavior { get; set; }

    /// <summary>
    /// Gets or sets the policy applied to the YAML merge key (<c>&lt;&lt;</c>).
    /// </summary>
    /// <value>The merge-key policy; the default is <see cref="YamlMergeKeyBehavior.Expand" />.</value>
    public YamlMergeKeyBehavior MergeKeyBehavior { get; set; }

    /// <summary>
    /// Gets or sets the maximum container nesting depth permitted while parsing.
    /// </summary>
    /// <value>
    /// The maximum depth. A value of zero or less selects the default of 64. Larger values are clamped to the
    /// implementation ceiling.
    /// </value>
    public int MaxDepth
    {
        readonly get => _maxDepth;
        set => _maxDepth = value;
    }

    /// <summary>
    /// Gets the effective maximum depth, applying the default and the implementation ceiling.
    /// </summary>
    /// <value>The clamped maximum nesting depth used by the parser.</value>
    internal readonly int EffectiveMaxDepth =>
        _maxDepth <= 0 ? YamlLimits.DefaultMaxDepth : Math.Min(_maxDepth, YamlLimits.AbsoluteMaxDepth);
}
