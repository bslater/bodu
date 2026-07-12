// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IOnSerializing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Defines a callback that the serializer invokes on a value immediately before its members are written, letting the
/// value prepare its state for serialization.
/// </summary>
/// <remarks>
/// <see cref="OnSerializing" /> is called after a non-<see langword="null" /> value has been selected for writing and
/// before its members are emitted, so any mutation it performs is reflected in the output.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Snapshot : IOnSerializing
/// {
///     public long SavedAt { get; set; }
///
///     void IOnSerializing.OnSerializing() =>
///         SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();   // stamped before members are written
/// }
///]]>
/// </code>
/// </example>
public interface IOnSerializing
{
    /// <summary>
    /// Called by the serializer before the value's members are written.
    /// </summary>
    void OnSerializing();
}
