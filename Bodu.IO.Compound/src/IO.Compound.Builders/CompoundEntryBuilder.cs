// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundEntryBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.IO.Compound.Builders;

/// <summary>
/// Represents a node in a mutable compound-file object model — either a storage (a named container) or a stream (a
/// named byte payload).
/// </summary>
/// <remarks>
/// <para>
/// This is the authoring counterpart of the read-only navigation surface (<see cref="CompoundStorage" /> /
/// <see cref="CompoundStream" />), shaped after the <c>JsonNode</c> family: build a tree of
/// <see cref="CompoundStorageBuilder" /> and <see cref="CompoundStreamBuilder" /> objects, then serialize it to a
/// compound file. A node belongs to at most one parent storage at a time.
/// </para>
/// <para>
/// Unlike a JSON document, a compound file has no array or null concept — every entry is a named storage or stream — so
/// the model has exactly two concrete node kinds.
/// </para>
/// </remarks>
public abstract class CompoundEntryBuilder
{
    /// <summary>The maximum length, in UTF-16 code units, of a compound-file entry name.</summary>
    internal const int MaxNameLength = 31;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundEntryBuilder" /> class.
    /// </summary>
    private protected CompoundEntryBuilder()
    {
    }

    /// <summary>
    /// Gets the name of the entry as it appears in its parent storage.
    /// </summary>
    /// <value>The entry name; the key under which the parent storage holds this node.</value>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the parent storage of this node, or <see langword="null" /> when the node is a detached root.
    /// </summary>
    /// <value>The parent <see cref="CompoundStorageBuilder" />, or <see langword="null" />.</value>
    public CompoundStorageBuilder? Parent { get; internal set; }

    /// <summary>
    /// Gets the topmost ancestor of this node.
    /// </summary>
    /// <value>The root node reached by following <see cref="Parent" />; this node when it has no parent.</value>
    public CompoundEntryBuilder Root
    {
        get
        {
            CompoundEntryBuilder current = this;
            while (current.Parent is not null)
                current = current.Parent;

            return current;
        }
    }

    /// <summary>
    /// Gets or sets the class identifier (CLSID) associated with the entry.
    /// </summary>
    /// <value>The entry's class identifier; <see cref="Guid.Empty" /> when none is assigned.</value>
    public Guid ClassId { get; set; }

    /// <summary>
    /// Gets or sets the creation time recorded for the entry.
    /// </summary>
    /// <value>The creation time, or <see langword="null" /> when none is recorded.</value>
    public DateTimeOffset? CreationTime { get; set; }

    /// <summary>
    /// Gets or sets the last-modified time recorded for the entry.
    /// </summary>
    /// <value>The last-modified time, or <see langword="null" /> when none is recorded.</value>
    public DateTimeOffset? ModifiedTime { get; set; }

    /// <summary>
    /// Gets or sets the user-defined state bits recorded for the entry.
    /// </summary>
    /// <value>The raw state-bit flags.</value>
    public int StateBits { get; set; }

    /// <summary>
    /// Gets the kind of entry this node represents.
    /// </summary>
    /// <value>The <see cref="CompoundEntryType" /> of the node.</value>
    public abstract CompoundEntryType EntryType { get; }

    /// <summary>
    /// Returns this node as a <see cref="CompoundStorageBuilder" />.
    /// </summary>
    /// <returns>This node cast to <see cref="CompoundStorageBuilder" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not a storage.</exception>
    public CompoundStorageBuilder AsStorage() =>
        this as CompoundStorageBuilder
            ?? throw new InvalidOperationException(CompoundResourceStrings.Op_Invalid_CompoundEntryBuilderNotStorage);

    /// <summary>
    /// Returns this node as a <see cref="CompoundStreamBuilder" />.
    /// </summary>
    /// <returns>This node cast to <see cref="CompoundStreamBuilder" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this node is not a stream.</exception>
    public CompoundStreamBuilder AsStream() =>
        this as CompoundStreamBuilder
            ?? throw new InvalidOperationException(CompoundResourceStrings.Op_Invalid_CompoundEntryBuilderNotStream);

    /// <summary>
    /// Creates a deep, detached copy of this node and its descendants.
    /// </summary>
    /// <returns>An independent copy with no parent.</returns>
    public abstract CompoundEntryBuilder DeepClone();

    /// <summary>
    /// Attaches this node to a parent storage, enforcing the single-parent invariant.
    /// </summary>
    /// <param name="parent">The storage taking ownership of this node.</param>
    /// <exception cref="InvalidOperationException">Thrown when this node already belongs to a storage.</exception>
    internal void AssignParent(CompoundStorageBuilder parent)
    {
        if (Parent is not null)
            throw new InvalidOperationException(CompoundResourceStrings.Op_Invalid_CompoundEntryBuilderAlreadyHasParent);

        Parent = parent;
    }

    /// <summary>
    /// Validates an entry name against the compound-file naming rules.
    /// </summary>
    /// <param name="name">The proposed entry name.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileSerializationException">
    /// Thrown when the name is empty, longer than 31 characters, or contains a reserved character.
    /// </exception>
    internal static void ValidateName(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        // Control-prefixed names (for example "SummaryInformation") are permitted, so only white space made
        // entirely of separators is rejected; an empty string is always rejected.
        if (name.Length == 0)
            throw new CompoundFileSerializationException(CompoundResourceStrings.Arg_Invalid_CompoundEntryBuilderNameEmpty);

        if (name.Length > MaxNameLength)
        {
            throw new CompoundFileSerializationException(
                string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Arg_Invalid_CompoundEntryBuilderNameTooLong, name));
        }

        foreach (char c in name)
        {
            if (c is '/' or '\\' or ':' or '!')
            {
                throw new CompoundFileSerializationException(
                    string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Arg_Invalid_CompoundEntryBuilderNameInvalidCharacter, name));
            }
        }
    }
}
