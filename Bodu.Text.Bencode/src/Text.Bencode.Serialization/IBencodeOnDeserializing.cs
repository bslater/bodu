// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IBencodeOnDeserializing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Defines a callback that the serializer invokes on a freshly constructed instance before its members are populated
/// from Bencode, letting the instance establish state ahead of member assignment.
/// </summary>
/// <remarks>
/// <see cref="OnDeserializing" /> is called after the instance has been constructed and before any settable member or
/// extension-data entry is assigned. For a type built through a parameterized constructor the instance does not exist
/// any earlier, so the callback necessarily runs after the constructor has consumed its bound arguments.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class TrackerConfig : IBencodeOnDeserializing
/// {
///     public int Port { get; set; }
///
///     void IBencodeOnDeserializing.OnDeserializing() =>
///         Port = 6881;   // the default that survives when the input omits the key
/// }
///]]>
/// </code>
/// </example>
public interface IBencodeOnDeserializing
{
    /// <summary>
    /// Called by the serializer after the instance is constructed and before its members are populated.
    /// </summary>
    void OnDeserializing();
}
