// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ITomlOnSerialized.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Defines a callback that the serializer invokes on a value immediately after its members have been written to TOML,
/// letting the value restore or release any state established for serialization.
/// </summary>
/// <remarks>
/// <see cref="OnSerialized" /> is called after the value's dictionary has been closed, so it observes the completed
/// write rather than influencing the emitted output.
/// </remarks>
public interface ITomlOnSerialized
{
    /// <summary>
    /// Called by the serializer after the value's members have been written.
    /// </summary>
    void OnSerialized();
}
