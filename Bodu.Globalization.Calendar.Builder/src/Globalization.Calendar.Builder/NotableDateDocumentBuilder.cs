// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Provides a fluent, chainable API for authoring a Bodu notable-date document on the v2 cookbook schema, with
/// full-fidelity XML serialization, a JSON-subset surface, and materialization into a
/// <see cref="NotableDateResource" /> through the canonical loader.
/// </summary>
/// <remarks>
/// <para>
/// The builder accumulates an in-memory model of the document. Calling <see cref="ToXml" /> or <see cref="Build()" />
/// renders the model to XML in the <c>urn:bodu:globalization:calendar</c> namespace; building then loads that XML
/// through <see cref="NotableDateResourceLoader" />, which remains the single source of validation and assembly. The
/// XML form expresses every feature of the schema, whereas the JSON form rendered by <see cref="ToJson" /> is
/// restricted to the subset the companion JSON schema can represent.
/// </para>
/// </remarks>
public sealed partial class NotableDateDocumentBuilder
{
    /// <summary>
    /// The metadata source entries, in declaration order.
    /// </summary>
    private readonly List<string> _sources = new();

    /// <summary>
    /// The adjustment policies, in declaration order.
    /// </summary>
    private readonly List<AdjustmentPolicyBuilder> _adjustmentPolicies = new();

    /// <summary>
    /// The imports, in declaration order.
    /// </summary>
    private readonly List<ImportBuilder> _imports = new();

    /// <summary>
    /// The notable-date concepts, in declaration order.
    /// </summary>
    private readonly List<NotableDateDefinitionBuilder> _definitions = new();

    /// <summary>
    /// The resource identifier, or <see langword="null" /> until one is supplied.
    /// </summary>
    private string? _resourceId;

    /// <summary>
    /// The schema version emitted on the root element.
    /// </summary>
    private string _schemaVersion = BuilderXml.DefaultSchemaVersion;

    /// <summary>
    /// The metadata name, or <see langword="null" /> when unset.
    /// </summary>
    private string? _metadataName;

    /// <summary>
    /// The metadata description, or <see langword="null" /> when unset.
    /// </summary>
    private string? _metadataDescription;

    /// <summary>
    /// The resolution-policy builder, or <see langword="null" /> when no policy is configured.
    /// </summary>
    private ResolutionPolicyBuilder? _resolutionPolicy;

    /// <summary>
    /// The override builder, or <see langword="null" /> when no override is configured.
    /// </summary>
    private OverrideBuilder? _overrides;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateDocumentBuilder" /> class.
    /// </summary>
    private NotableDateDocumentBuilder()
    {
    }

    /// <summary>
    /// Creates an empty document builder with no resource identifier configured.
    /// </summary>
    /// <returns>A new <see cref="NotableDateDocumentBuilder" />.</returns>
    public static NotableDateDocumentBuilder Create() =>
        new();

    /// <summary>
    /// Creates a document builder with the supplied resource identifier and schema version.
    /// </summary>
    /// <param name="resourceId">The resource identifier of the document.</param>
    /// <param name="schemaVersion">The schema version emitted on the root element.</param>
    /// <returns>A new <see cref="NotableDateDocumentBuilder" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="resourceId" /> or <paramref name="schemaVersion" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public static NotableDateDocumentBuilder Create(string resourceId, string schemaVersion = "1.0")
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(resourceId);
        ThrowHelper.ThrowIfNullOrWhiteSpace(schemaVersion);

        return new NotableDateDocumentBuilder
        {
            _resourceId = resourceId,
            _schemaVersion = schemaVersion,
        };
    }

    /// <summary>
    /// Sets the resource identifier of the document.
    /// </summary>
    /// <param name="resourceId">The resource identifier.</param>
    /// <returns>The same <see cref="NotableDateDocumentBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="resourceId" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public NotableDateDocumentBuilder WithResourceId(string resourceId)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(resourceId);

        this._resourceId = resourceId;
        return this;
    }

    /// <summary>
    /// Sets the schema version emitted on the root element.
    /// </summary>
    /// <param name="schemaVersion">The schema version.</param>
    /// <returns>The same <see cref="NotableDateDocumentBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="schemaVersion" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public NotableDateDocumentBuilder WithSchemaVersion(string schemaVersion)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(schemaVersion);

        this._schemaVersion = schemaVersion;
        return this;
    }

    /// <summary>
    /// Sets the document metadata name, description, and sources.
    /// </summary>
    /// <param name="name">The metadata name, or <see langword="null" /> to leave it unset.</param>
    /// <param name="description">The metadata description, or <see langword="null" /> to leave it unset.</param>
    /// <param name="sources">The metadata source entries.</param>
    /// <returns>The same <see cref="NotableDateDocumentBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sources" /> is <see langword="null" />.</exception>
    public NotableDateDocumentBuilder WithMetadata(string? name = null, string? description = null, params string[] sources)
    {
        ThrowHelper.ThrowIfNull(sources);

        this._metadataName = name;
        this._metadataDescription = description;
        this._sources.Clear();
        this._sources.AddRange(sources);
        return this;
    }

    /// <summary>
    /// Configures the document-level resolution policy through the supplied delegate.
    /// </summary>
    /// <param name="configure">A delegate that configures the resolution policy.</param>
    /// <returns>The same <see cref="NotableDateDocumentBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    public NotableDateDocumentBuilder WithResolutionPolicy(Action<ResolutionPolicyBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        ResolutionPolicyBuilder policy = this._resolutionPolicy ?? new ResolutionPolicyBuilder();
        configure(policy);
        this._resolutionPolicy = policy;
        return this;
    }

    /// <summary>
    /// Adds a reusable adjustment policy and configures it through the supplied delegate.
    /// </summary>
    /// <param name="id">The stable identifier of the policy.</param>
    /// <param name="configure">A delegate that configures the policy.</param>
    /// <returns>The same <see cref="NotableDateDocumentBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="id" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    public NotableDateDocumentBuilder AddAdjustmentPolicy(string id, Action<AdjustmentPolicyBuilder> configure)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(id);
        ThrowHelper.ThrowIfNull(configure);

        AdjustmentPolicyBuilder policy = new(id);
        configure(policy);
        this._adjustmentPolicies.Add(policy);
        return this;
    }

    /// <summary>
    /// Adds an import of an external resource and optionally configures its <c>Use</c> directives.
    /// </summary>
    /// <param name="resource">The name of the imported resource.</param>
    /// <param name="configure">A delegate that configures the import, or <see langword="null" />.</param>
    /// <returns>The same <see cref="NotableDateDocumentBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="resource" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    public NotableDateDocumentBuilder AddImport(string resource, Action<ImportBuilder>? configure = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(resource);

        ImportBuilder import = new(resource);
        configure?.Invoke(import);
        this._imports.Add(import);
        return this;
    }

    /// <summary>
    /// Adds a notable-date concept and configures it through the supplied delegate.
    /// </summary>
    /// <param name="id">The stable identifier of the concept.</param>
    /// <param name="displayName">The human-readable display name of the concept.</param>
    /// <param name="category">The category of the concept.</param>
    /// <param name="configure">A delegate that configures the concept.</param>
    /// <returns>The same <see cref="NotableDateDocumentBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="id" /> or <paramref name="displayName" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    public NotableDateDocumentBuilder AddNotableDate(string id, string displayName, NotableDateCategory category, Action<NotableDateDefinitionBuilder> configure)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(id);
        ThrowHelper.ThrowIfNullOrWhiteSpace(displayName);
        ThrowHelper.ThrowIfNull(configure);

        NotableDateDefinitionBuilder definition = new(id, displayName, category);
        configure(definition);
        this._definitions.Add(definition);
        return this;
    }

    /// <summary>
    /// Adds override operations through the supplied delegate, appending to any previously configured overrides.
    /// </summary>
    /// <param name="configure">A delegate that configures the override operations.</param>
    /// <returns>The same <see cref="NotableDateDocumentBuilder" /> instance, enabling chained calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    public NotableDateDocumentBuilder AddOverride(Action<OverrideBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        OverrideBuilder overrides = this._overrides ?? new OverrideBuilder();
        configure(overrides);
        this._overrides = overrides;
        return this;
    }

    /// <summary>
    /// Materializes the document into a validated <see cref="NotableDateResource" /> through the canonical loader.
    /// </summary>
    /// <returns>The loaded and validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// The document is incomplete: the resource identifier is missing, a concept has no rules, a rule has no strategy,
    /// or an adjustment policy is missing a trigger, action, or emission.
    /// </exception>
    /// <exception cref="NotableDateValidationException">The loader produced one or more error diagnostics.</exception>
    public NotableDateResource Build() =>
        this.Build(null);

    /// <summary>
    /// Materializes the document into a validated <see cref="NotableDateResource" />, resolving imports through the
    /// supplied resolver.
    /// </summary>
    /// <param name="importResolver">
    /// A delegate mapping a resource name to its XML or JSON content, or <see langword="null" /> to resolve no imports.
    /// </param>
    /// <returns>The loaded and validated <see cref="NotableDateResource" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// The document is incomplete: the resource identifier is missing, a concept has no rules, a rule has no strategy,
    /// or an adjustment policy is missing a trigger, action, or emission.
    /// </exception>
    /// <exception cref="NotableDateValidationException">The loader produced one or more error diagnostics.</exception>
    public NotableDateResource Build(Func<string, string?>? importResolver) =>
        NotableDateResourceLoader.Load(this.ToXml(), importResolver ?? (_ => null));

    /// <summary>
    /// Materializes the document and wraps it in a mutable provider.
    /// </summary>
    /// <returns>An <see cref="INotableDateResourceProvider" /> exposing the built resource.</returns>
    /// <exception cref="InvalidOperationException">The document is incomplete.</exception>
    /// <exception cref="NotableDateValidationException">The loader produced one or more error diagnostics.</exception>
    public INotableDateResourceProvider ToProvider() =>
        new MutableNotableDateResourceProvider(this.Build());

    /// <summary>
    /// Serializes the document to a file, inferring the format from the file extension.
    /// </summary>
    /// <param name="path">
    /// The destination file path. A <c>.xml</c> extension writes XML; a <c>.json</c> extension writes JSON.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is <see langword="null" />, empty, white-space, or has an unrecognized extension.
    /// </exception>
    /// <exception cref="InvalidOperationException">The document is incomplete.</exception>
    /// <exception cref="NotSupportedException">
    /// A JSON target is requested but the document uses a feature the JSON subset cannot represent.
    /// </exception>
    public void Save(string path)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        this.Save(path, InferFormat(path));
    }

    /// <summary>
    /// Serializes the document to a file in the specified format.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="format">The serialization format.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="InvalidOperationException">The document is incomplete.</exception>
    /// <exception cref="NotSupportedException">
    /// A JSON target is requested but the document uses a feature the JSON subset cannot represent.
    /// </exception>
    public void Save(string path, NotableDateDocumentFormat format)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        string content = format == NotableDateDocumentFormat.Json ? this.ToJson() : this.ToXml();
        File.WriteAllText(path, content);
    }

    /// <summary>
    /// Creates a deep copy of this document builder.
    /// </summary>
    /// <returns>
    /// A new <see cref="NotableDateDocumentBuilder" /> carrying an independent copy of the document model.
    /// </returns>
    public NotableDateDocumentBuilder Clone()
    {
        NotableDateDocumentBuilder clone = new()
        {
            _resourceId = this._resourceId,
            _schemaVersion = this._schemaVersion,
            _metadataName = this._metadataName,
            _metadataDescription = this._metadataDescription,
            _resolutionPolicy = this._resolutionPolicy?.Clone(),
            _overrides = this._overrides?.Clone(),
        };

        clone._sources.AddRange(this._sources);
        foreach (AdjustmentPolicyBuilder policy in this._adjustmentPolicies)
            clone._adjustmentPolicies.Add(policy.Clone());
        foreach (ImportBuilder import in this._imports)
            clone._imports.Add(import.Clone());
        foreach (NotableDateDefinitionBuilder definition in this._definitions)
            clone._definitions.Add(definition.Clone());

        return clone;
    }

    /// <summary>
    /// Reads a document from a file and parses it into a builder, inferring the format from the file extension.
    /// </summary>
    /// <param name="path">
    /// The source file path. A <c>.xml</c> extension reads XML; a <c>.json</c> extension reads JSON.
    /// </param>
    /// <returns>A new <see cref="NotableDateDocumentBuilder" /> populated from the file.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="path" /> is <see langword="null" />, empty, white-space, or has an unrecognized extension.
    /// </exception>
    /// <exception cref="FormatException">The file content is not a well-formed notable-date document.</exception>
    public static NotableDateDocumentBuilder Load(string path)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(path);

        string content = File.ReadAllText(path);
        return InferFormat(path) == NotableDateDocumentFormat.Json ? FromJson(content) : FromXml(content);
    }

    /// <summary>
    /// Resolves the resource identifier, failing when it has not been configured.
    /// </summary>
    /// <returns>The configured resource identifier.</returns>
    /// <exception cref="InvalidOperationException">No resource identifier has been configured.</exception>
    private string RequireResourceId() =>
        string.IsNullOrWhiteSpace(this._resourceId)
            ? throw new InvalidOperationException(BuilderResourceStrings.Op_Invalid_DocumentResourceIdNotSet)
            : this._resourceId!;

    /// <summary>
    /// Infers the serialization format from a file path's extension.
    /// </summary>
    /// <param name="path">The file path to inspect.</param>
    /// <returns>The inferred serialization format.</returns>
    /// <exception cref="ArgumentException">The extension is neither <c>.xml</c> nor <c>.json</c>.</exception>
    private static NotableDateDocumentFormat InferFormat(string path)
    {
        string extension = Path.GetExtension(path);

        if (string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase))
            return NotableDateDocumentFormat.Xml;

        if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
            return NotableDateDocumentFormat.Json;

        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, BuilderResourceStrings.Arg_Invalid_UnknownFileExtension, path), nameof(path));
    }
}
