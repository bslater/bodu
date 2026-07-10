// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolveOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how a <see cref="ConfigurationDocument" /> is projected into a <see cref="ConfigurationView" /> for a
/// specific target path.
/// </summary>
/// <remarks>
/// <para>
/// Resolution is path-aware: each section in the document either matches the supplied target path (under EditorConfig
/// glob semantics) or does not, and the resolved view layers the preamble and every matching section in source order,
/// with the last matching section's value winning. This options bag controls every variable that influences that
/// layering — which path root anchors anchored globs, whether preamble properties contribute, which string comparison
/// is used for path matching, and how the EditorConfig <c>unset</c> sentinel is handled.
/// </para>
/// <para>
/// <see cref="PathRoot" /> deserves attention when the document was parsed from a string rather than loaded from a
/// file. With no path on the document and <see cref="PathRoot" /> left <see langword="null" />, the resolver consults
/// <see cref="MissingPathRootMode" /> to decide whether to use the empty root, throw, or fall back to the target path's
/// parent — that choice changes which sections match.
/// </para>
/// <para>
/// <see cref="KeyOptions" /> should normally be the same instance passed to
/// <see cref="ConfigurationParseOptions.KeyOptions" /> so that lookups against the resolved view use the same comparer
/// and mapping that produced the parsed keys. The default <see cref="ConfigurationKeyOptions.Default" /> matches the
/// <c>Microsoft.Extensions.Configuration</c> shape.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// ConfigurationView view = document.Resolve(
///     "src/App/Program.cs",
///     new ConfigurationResolveOptions
///     {
///         Profile = ConfigurationProfile.EditorConfigCompatible,
///         UnsetValueMode = ConfigurationUnsetValueMode.RemoveEffectiveValue,
///     });
///
/// // Or start from a canonical profile option set and rely on its coherent defaults:
/// ConfigurationView strict = document.Resolve(null, ConfigurationResolveOptions.For(ConfigurationProfile.Strict));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed partial class ConfigurationResolveOptions
{
    /// <summary>
    /// Gets the behaviour profile this option bag represents.
    /// </summary>
    /// <value>The selected profile.</value>
    public ConfigurationProfile Profile { get; init; } = ConfigurationProfile.Bodu;

    /// <summary>
    /// Gets the optional path root used to evaluate anchored glob patterns. When <see langword="null" /> the document's
    /// load path is used; if neither is available, <see cref="MissingPathRootMode" /> controls behaviour.
    /// </summary>
    /// <value>The path root, or <see langword="null" />.</value>
    public string? PathRoot { get; init; }

    /// <summary>
    /// Gets how the resolver reacts to an absent <see cref="PathRoot" /> when the document was parsed from a string.
    /// </summary>
    /// <value>The selected mode.</value>
    public ConfigurationMissingPathRootMode MissingPathRootMode { get; init; } =
        ConfigurationMissingPathRootMode.UseEmptyRoot;

    /// <summary>
    /// Gets a value indicating whether preamble (global) properties contribute to resolution. Defaults to
    /// <see langword="true" /> for the Bodu profile and <see langword="false" /> for EditorConfig-compatible.
    /// </summary>
    /// <value><see langword="true" /> when preamble properties are honoured.</value>
    public bool ApplyPreambleProperties { get; init; } = true;

    /// <summary>
    /// Gets the comparison used when matching target paths against section patterns.
    /// </summary>
    /// <value>The selected <see cref="StringComparison" />.</value>
    public StringComparison PathComparison { get; init; } = StringComparison.Ordinal;

    /// <summary>
    /// Gets the unset-value handling mode used by the resolver.
    /// </summary>
    /// <value>The selected unset-value mode.</value>
    public ConfigurationUnsetValueMode UnsetValueMode { get; init; } =
        ConfigurationUnsetValueMode.TreatAsLiteral;

    /// <summary>
    /// Gets the key options used when expanding raw keys into configuration keys for the resolved view.
    /// </summary>
    /// <value>The selected key options.</value>
    public ConfigurationKeyOptions KeyOptions { get; init; } = ConfigurationKeyOptions.Default;
}
