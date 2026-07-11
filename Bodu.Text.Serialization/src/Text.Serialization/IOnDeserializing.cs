// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IOnDeserializing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Defines a callback that the serializer invokes on a freshly constructed value immediately before its members are
/// populated from the input, letting the value initialize default state.
/// </summary>
/// <remarks>
/// <see cref="OnDeserializing" /> is called after the target instance has been created and before any member is
/// assigned, so state it establishes can be overwritten by the incoming data.
/// </remarks>
public interface IOnDeserializing
{
    /// <summary>
    /// Called by the serializer before the value's members are populated.
    /// </summary>
    void OnDeserializing();
}
