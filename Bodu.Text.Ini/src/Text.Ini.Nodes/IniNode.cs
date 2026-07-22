// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;

using Bodu.Text.Ini.Reader;
using Bodu.Text.Ini.Writer;

namespace Bodu.Text.Ini.Nodes;

/// <summary>
/// Represents a node in a mutable INI document tree. An INI document is a two-level object-of-objects: the root
/// <see cref="IniObject" /> maps global keys to <see cref="IniValue" /> leaves and section names to section
/// <see cref="IniObject" /> instances, and each section maps key names to <see cref="IniValue" /> leaves.
/// </summary>
/// <remarks>
/// Unlike the other quartet DOMs, this tree deliberately bears comment trivia so that authoring and round-tripping are
/// faithful: <see cref="LeadingComments" /> holds the comment lines that precede a node, and each
/// <see cref="IniObject" /> additionally holds the trailing comment block at the end of its scope. Inline comments are
/// not modeled — the reader dialect keeps everything after the assignment as part of the value, so an emitted inline
/// comment would be re-read as value text.
/// </remarks>
public abstract class IniNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IniNode" /> class.
    /// </summary>
    private protected IniNode()
    {
        LeadingComments = [];
    }

    /// <summary>
    /// Gets the value kind of this node.
    /// </summary>
    /// <value>The node's <see cref="IniValueKind" />.</value>
    public abstract IniValueKind ValueKind { get; }

    /// <summary>
    /// Gets the comment lines that precede this node, without their leading comment prefix character.
    /// </summary>
    /// <value>The mutable leading-comment list; empty when the node has no leading comments.</value>
    /// <remarks>
    /// Leading comments are emitted by the containing object when the node is written, each as one comment line. The
    /// outermost object written as a document emits no leading comments of its own; document-level trivia attaches to
    /// the first entry or section instead.
    /// </remarks>
    public IList<string> LeadingComments { get; }

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a mutable INI object tree using the default reader and document options.
    /// </summary>
    /// <param name="utf8Ini">The INI source bytes.</param>
    /// <returns>The parsed <see cref="IniObject" /> root.</returns>
    /// <exception cref="IniFormatException">Thrown when the bytes are not valid INI.</exception>
    public static IniObject Parse(ReadOnlySpan<byte> utf8Ini) =>
        Parse(utf8Ini, IniReaderOptions.Default, IniDocumentOptions.Default);

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a mutable INI object tree using the supplied reader and document options.
    /// </summary>
    /// <param name="utf8Ini">The INI source bytes.</param>
    /// <param name="readerOptions">The source-order reader options.</param>
    /// <param name="documentOptions">The document-model duplicate policies.</param>
    /// <returns>The parsed <see cref="IniObject" /> root.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when the bytes are not valid INI, or when a duplicate section, duplicate key, or global-key/section-name
    /// collision violates the configured policies.
    /// </exception>
    public static IniObject Parse(ReadOnlySpan<byte> utf8Ini, IniReaderOptions readerOptions, IniDocumentOptions documentOptions)
    {
        var reader = new Utf8IniReader(utf8Ini, readerOptions);
        var root = new IniObject();
        IniObject current = root;
        string currentName = string.Empty;
        var pendingComments = new List<string>();
        string? pendingKey = null;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case IniTokenType.Comment:
                    pendingComments.Add(reader.GetString());
                    break;

                case IniTokenType.SectionHeader:
                    current = ResolveSection(root, reader.GetString(), documentOptions, pendingComments, ref reader);
                    currentName = reader.GetString();
                    pendingComments.Clear();
                    break;

                case IniTokenType.PropertyName:
                    pendingKey = reader.GetString();
                    break;

                case IniTokenType.String:
                    var value = new IniValue(reader.GetString());
                    foreach (string comment in pendingComments)
                        value.LeadingComments.Add(comment);

                    ApplyEntry(current, currentName, pendingKey!, value, documentOptions.DuplicateKeyBehavior, ref reader);
                    pendingComments.Clear();
                    pendingKey = null;
                    break;

                default:
                    break;
            }
        }

        foreach (string comment in pendingComments)
            current.TrailingComments.Add(comment);

        return root;
    }

    /// <summary>
    /// Parses the supplied INI text into a mutable INI object tree.
    /// </summary>
    /// <param name="ini">The INI source text.</param>
    /// <returns>The parsed <see cref="IniObject" /> root.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ini" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="IniFormatException">Thrown when the text is not valid INI.</exception>
    public static IniObject Parse(string ini)
    {
        ThrowHelper.ThrowIfNull(ini);

        return Parse(Encoding.UTF8.GetBytes(ini));
    }

    /// <summary>
    /// Returns this node as an <see cref="IniObject" />.
    /// </summary>
    /// <returns>This node cast to <see cref="IniObject" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not an object.</exception>
    public IniObject AsObject() =>
        this as IniObject ?? throw new InvalidOperationException();

    /// <summary>
    /// Returns this node as an <see cref="IniValue" />.
    /// </summary>
    /// <returns>This node cast to <see cref="IniValue" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not a value.</exception>
    public IniValue AsValue() =>
        this as IniValue ?? throw new InvalidOperationException();

    /// <summary>
    /// Creates a deep copy of this node, including its comment trivia.
    /// </summary>
    /// <returns>The cloned node.</returns>
    public abstract IniNode DeepClone();

    /// <summary>
    /// Writes this node's INI representation to the supplied writer.
    /// </summary>
    /// <param name="writer">The writer that receives the INI bytes.</param>
    public abstract void WriteTo(ref Utf8IniWriter writer);

    /// <summary>
    /// Serializes this node to a new UTF-8 byte array.
    /// </summary>
    /// <returns>The INI bytes.</returns>
    public byte[] ToUtf8Bytes()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8IniWriter(buffer);
        WriteTo(ref writer);
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Serializes this node to INI text.
    /// </summary>
    /// <returns>The INI text.</returns>
    public override string ToString() =>
        Encoding.UTF8.GetString(ToUtf8Bytes());

    /// <summary>
    /// Converts a string to an <see cref="IniValue" />.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator IniNode(string value) =>
        new IniValue(value);

    /// <summary>
    /// Resolves a section header against the root object: reuses or merges an existing section per the
    /// duplicate-section policy, or creates and registers a new one carrying the pending leading comments.
    /// </summary>
    /// <param name="root">The document root object.</param>
    /// <param name="name">The section name.</param>
    /// <param name="documentOptions">The document-model duplicate policies.</param>
    /// <param name="pendingComments">The comment lines read since the previous node.</param>
    /// <param name="reader">The reader, for the source position of a policy violation.</param>
    /// <returns>The section object that subsequent entries belong to.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when the name collides with a global key, or repeats under
    /// <see cref="IniDuplicateSectionBehavior.Disallowed" />.
    /// </exception>
    private static IniObject ResolveSection(
        IniObject root,
        string name,
        IniDocumentOptions documentOptions,
        List<string> pendingComments,
        ref Utf8IniReader reader)
    {
        if (root.TryGetValue(name, out IniNode? existing))
        {
            if (existing is IniValue)
            {
                throw new IniFormatException(
                    string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniGlobalKeyCollision, name),
                    reader.LineNumber,
                    reader.BytesConsumed);
            }

            if (documentOptions.DuplicateSectionBehavior == IniDuplicateSectionBehavior.Disallowed)
            {
                throw new IniFormatException(
                    string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniDuplicateSection, name),
                    reader.LineNumber,
                    reader.BytesConsumed);
            }

            var merged = (IniObject)existing;
            foreach (string comment in pendingComments)
                merged.LeadingComments.Add(comment);

            return merged;
        }

        var section = new IniObject();
        foreach (string comment in pendingComments)
            section.LeadingComments.Add(comment);

        root.SetNode(name, section);
        return section;
    }

    /// <summary>
    /// Applies one parsed entry to its owning object, resolving a duplicate key by the configured policy.
    /// </summary>
    /// <param name="target">The object the entry belongs to.</param>
    /// <param name="targetName">The owning section name, or an empty string for the global section.</param>
    /// <param name="key">The entry key.</param>
    /// <param name="value">The parsed value node, already carrying its leading comments.</param>
    /// <param name="behavior">The duplicate-key policy.</param>
    /// <param name="reader">The reader, for the source position of a policy violation.</param>
    /// <exception cref="IniFormatException">
    /// Thrown when the key is a duplicate and the policy is <see cref="IniDuplicateKeyBehavior.Disallowed" />.
    /// </exception>
    private static void ApplyEntry(
        IniObject target,
        string targetName,
        string key,
        IniValue value,
        IniDuplicateKeyBehavior behavior,
        ref Utf8IniReader reader)
    {
        if (target.ContainsKey(key))
        {
            switch (behavior)
            {
                case IniDuplicateKeyBehavior.LastWins:
                    target.SetNode(key, value);
                    break;

                case IniDuplicateKeyBehavior.FirstWins:
                    break;

                default:
                    throw new IniFormatException(
                        string.Format(CultureInfo.CurrentCulture, IniResourceStrings.Format_Invalid_IniDuplicateKey, targetName, key),
                        reader.LineNumber,
                        reader.BytesConsumed);
            }

            return;
        }

        target.SetNode(key, value);
    }
}
