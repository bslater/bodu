// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ITomlOnSerializing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Defines a callback that the serializer invokes on a value immediately before its members are written to TOML,
/// letting the value prepare its state for serialization.
/// </summary>
/// <remarks>
/// <see cref="OnSerializing" /> is called after a non-<see langword="null" /> value has been selected for writing and
/// before the opening of its dictionary, so any mutation it performs is reflected in the emitted output.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Snapshot : ITomlOnSerializing
/// {
///     public DateTime SavedAt { get; set; }
///
///     void ITomlOnSerializing.OnSerializing() =>
///         SavedAt = DateTime.UtcNow;   // stamped before members are written, so it appears in the output
/// }
///]]>
/// </code>
/// </example>
public interface ITomlOnSerializing
{
    /// <summary>
    /// Called by the serializer before the value's members are written.
    /// </summary>
    void OnSerializing();
}
