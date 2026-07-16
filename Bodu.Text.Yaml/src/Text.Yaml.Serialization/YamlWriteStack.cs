// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlWriteStack.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Carries the serializer's traversal state across the recursive converter calls of a single write: the path to the
/// value currently being written, the set of reference instances in progress (for cycle detection), and the first
/// failure recorded while writing.
/// </summary>
/// <remarks>
/// <para>
/// The converters report an over-deep graph or a reference cycle by recording the failure here and returning, rather
/// than throwing from deep in the recursion and rethrowing through every parent frame. The single recorded failure is
/// thrown once by the serializer engine after control has returned to the root, so the call stack unwinds through
/// normal returns and cannot exhaust a constrained stack while dispatching the exception.
/// </para>
/// <para>
/// Depth is intentionally <em>not</em> tracked here. The writer is the single owner of container depth (
/// <c>Utf8YamlWriter.CurrentDepth</c>); a converter consults it directly before opening a container, mirroring how
/// <c>System.Text.Json</c>'s serializer reads <c>Utf8JsonWriter.CurrentDepth</c>. This avoids a second counter that
/// could drift, and ensures pass-through converters that re-dispatch without opening a container do not consume a
/// level.
/// </para>
/// </remarks>
internal sealed class YamlWriteStack
{
    /// <summary>The path segments for every open container level, innermost last. Each entry is a member or mapping key, or a sequence index formatted only if a failure is recorded.</summary>
    private readonly List<PathSegment> _path = [];

    /// <summary>The reference instances currently being written, used to detect an object cycle by reference identity.</summary>
    private readonly HashSet<object> _references = new(ReferenceEqualityComparer.Instance);

    /// <summary>The first failure recorded during the write, or <see langword="null" /> while none has occurred.</summary>
    private YamlSerializationException? _failure;

    /// <summary>
    /// Gets a value indicating whether a failure has been recorded, after which converters cooperatively stop work and
    /// unwind.
    /// </summary>
    /// <value><see langword="true" /> when a failure has been recorded; otherwise <see langword="false" />.</value>
    internal bool HasFailure => _failure is not null;

    /// <summary>
    /// Pushes a path segment for a container the writer is about to descend into.
    /// </summary>
    /// <param name="segment">The member or mapping key.</param>
    internal void PushPath(string segment) =>
        _path.Add(new PathSegment(segment));

    /// <summary>
    /// Pushes a sequence-index path segment for a container the writer is about to descend into. The index is stored
    /// unformatted, so the success path allocates nothing; the <c>[i]</c> text is produced only when a failure
    /// captures the path.
    /// </summary>
    /// <param name="index">The zero-based sequence index.</param>
    internal void PushPath(int index) =>
        _path.Add(new PathSegment(index));

    /// <summary>
    /// Pops the most recently pushed path segment after the corresponding value has been written successfully.
    /// </summary>
    internal void PopPath() =>
        _path.RemoveAt(_path.Count - 1);

    /// <summary>
    /// Records that the specified reference-typed value is being written, reporting whether it was already in progress.
    /// </summary>
    /// <param name="value">The reference being entered.</param>
    /// <returns>
    /// <see langword="true" /> when the reference was newly recorded; <see langword="false" /> when it is already being
    /// written, which indicates an object cycle.
    /// </returns>
    internal bool TryEnterReference(object value) =>
        _references.Add(value);

    /// <summary>
    /// Removes the specified reference-typed value from the in-progress set once it has been fully written.
    /// </summary>
    /// <param name="value">The reference being exited.</param>
    internal void ExitReference(object value) =>
        _references.Remove(value);

    /// <summary>
    /// Records a failure, capturing the path to the value currently being written. Only the first failure is retained.
    /// </summary>
    /// <param name="message">The fully formatted, culture-aware failure message.</param>
    /// <remarks>
    /// The path is captured from the live segment stack at the instant of detection — the deepest point of the
    /// traversal, before any parent has unwound — so it names the value where the failure occurred.
    /// </remarks>
    internal void SetFailure(string message) =>
        _failure ??= new YamlSerializationException(message) { Path = BuildPath() };

    /// <summary>
    /// Throws the recorded failure, if any. Called once at the root of the write after control has safely returned.
    /// </summary>
    /// <exception cref="YamlSerializationException">Thrown when a failure was recorded during the write.</exception>
    internal void ThrowIfFailed()
    {
        if (_failure is { } failure)
            throw failure;
    }

    /// <summary>
    /// Builds the dotted, index-aware path from the current segment stack using the same join rules as
    /// <see cref="YamlSerializationException.CombinePath" />.
    /// </summary>
    /// <returns>The combined path, or <see langword="null" /> when the failure occurred at the document root.</returns>
    private string? BuildPath()
    {
        if (_path.Count == 0)
            return null;

        string? path = null;
        for (int i = _path.Count - 1; i >= 0; i--)
            path = YamlSerializationException.CombinePath(_path[i].Format(), path);

        return path;
    }

    /// <summary>
    /// A path segment: a member or mapping key, or an unformatted sequence index.
    /// </summary>
    private readonly struct PathSegment
    {
        /// <summary>The key text, or <see langword="null" /> for an index segment.</summary>
        private readonly string? _key;

        /// <summary>The sequence index, meaningful only when <see cref="_key" /> is <see langword="null" />.</summary>
        private readonly int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathSegment" /> struct for a key segment.
        /// </summary>
        /// <param name="key">The member or mapping key.</param>
        internal PathSegment(string key)
        {
            _key = key;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathSegment" /> struct for a sequence-index segment.
        /// </summary>
        /// <param name="index">The zero-based sequence index.</param>
        internal PathSegment(int index)
        {
            _index = index;
        }

        /// <summary>
        /// Produces the segment's path text: the key, or the index in its <c>[i]</c> form.
        /// </summary>
        /// <returns>The segment text.</returns>
        internal string Format() =>
            _key ?? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"[{_index}]");
    }
}
