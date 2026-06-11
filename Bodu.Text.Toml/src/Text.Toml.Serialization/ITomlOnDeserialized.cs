// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ITomlOnDeserialized.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Defines a callback that the serializer invokes on an instance after all of its members have been populated from
/// TOML, letting the instance validate or finalize its state.
/// </summary>
/// <remarks>
/// <see cref="OnDeserialized" /> is the last step of deserialization for the instance: it runs after every settable
/// member and any extension-data entry has been assigned, so it observes the fully materialized object.
/// </remarks>
public interface ITomlOnDeserialized
{
    /// <summary>
    /// Called by the serializer after the instance's members have been populated.
    /// </summary>
    void OnDeserialized();
}
