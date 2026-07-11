// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IOnSerialized.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Defines a callback that the serializer invokes on a value immediately after its members have been written, letting
/// the value restore or release any state prepared for serialization.
/// </summary>
/// <remarks>
/// <see cref="OnSerialized" /> is called once the value's members have been emitted, so it observes the fully written
/// form and runs even when serialization of a subsequent value fails.
/// </remarks>
public interface IOnSerialized
{
    /// <summary>
    /// Called by the serializer after the value's members have been written.
    /// </summary>
    void OnSerialized();
}
