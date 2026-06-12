// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IBencodeOnDeserialized.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Defines a callback that the serializer invokes on an instance after all of its members have been populated from
/// Bencode, letting the instance validate or finalize its state.
/// </summary>
/// <remarks>
/// <see cref="OnDeserialized" /> is the last step of deserialization for the instance: it runs after every settable
/// member and any extension-data entry has been assigned, so it observes the fully materialized object.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class TrackerConfig : IBencodeOnDeserialized
/// {
///     public int Port { get; set; }
///
///     void IBencodeOnDeserialized.OnDeserialized()
///     {
///         if (Port is < 1 or > 65535) throw new InvalidOperationException("Port is out of range.");
///     }
/// }
///]]>
/// </code>
/// </example>
public interface IBencodeOnDeserialized
{
    /// <summary>
    /// Called by the serializer after the instance's members have been populated.
    /// </summary>
    void OnDeserialized();
}
