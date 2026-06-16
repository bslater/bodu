// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlWriteStack.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Carries the serializer's traversal state across the recursive converter calls of a single write: the path to the
/// value currently being written, the set of reference instances in progress (for cycle detection), and the first
/// failure recorded while writing.
/// </summary>
/// <remarks>
/// <para>
/// The converters report an over-deep graph or a reference cycle by recording the failure here and returning, rather
/// than throwing from deep in the recursion and rethrowing through every parent frame. The single recorded failure is
/// thrown once by <see cref="TomlSerializerEngine.Serialize{T}" /> after control has returned to the root, so the call
/// stack unwinds through normal returns and cannot exhaust a constrained stack while dispatching the exception.
/// </para>
/// <para>
/// Depth is intentionally <em>not</em> tracked here. The writer is the single owner of container depth (
/// <c>Utf8TomlWriter.Depth</c>); a converter consults it directly before opening a container, mirroring how
/// <c>System.Text.Json</c>'s serializer reads <c>Utf8JsonWriter.CurrentDepth</c>. This avoids a second counter that
/// could drift, and ensures pass-through converters that re-dispatch without opening a container do not consume a
/// level.
/// </para>
/// </remarks>
internal sealed class TomlWriteStack
{
    /// <summary>
    /// The path segments for every open container level, innermost last. Each entry is a member or dictionary key, or
    /// an array index of the form <c>[i]</c>.
    /// </summary>
    private readonly List<string> _path = [];

    /// <summary>
    /// The reference instances currently being written, used to detect an object cycle by reference identity.
    /// </summary>
    private readonly HashSet<object> _references = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// The first failure recorded during the write, or <see langword="null" /> while none has occurred.
    /// </summary>
    private TomlSerializationException? _failure;

    /// <summary>
    /// Gets a value indicating whether a failure has been recorded, after which converters cooperatively stop work and
    /// unwind.
    /// </summary>
    /// <returns><see langword="true" /> when a failure has been recorded; otherwise <see langword="false" />.</returns>
    internal bool HasFailure => _failure is not null;

    /// <summary>
    /// Pushes a path segment for a container the writer is about to descend into.
    /// </summary>
    /// <param name="segment">The member or dictionary key, or an array index of the form <c>[i]</c>.</param>
    internal void PushPath(string segment) =>
        _path.Add(segment);

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
        _failure ??= new TomlSerializationException(message) { Path = BuildPath() };

    /// <summary>
    /// Throws the recorded failure, if any. Called once at the root of the write after control has safely returned.
    /// </summary>
    /// <exception cref="TomlSerializationException">Thrown when a failure was recorded during the write.</exception>
    internal void ThrowIfFailed()
    {
        if (_failure is { } failure)
            throw failure;
    }

    /// <summary>
    /// Builds the dotted, index-aware path from the current segment stack using the same join rules as
    /// <see cref="TomlSerializationException.CombinePath" />.
    /// </summary>
    /// <returns>The combined path, or <see langword="null" /> when the failure occurred at the document root.</returns>
    private string? BuildPath()
    {
        if (_path.Count == 0)
            return null;

        string? path = null;
        for (int i = _path.Count - 1; i >= 0; i--)
            path = TomlSerializationException.CombinePath(_path[i], path);

        return path;
    }
}
