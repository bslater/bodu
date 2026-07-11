// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IOnDeserialized.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Defines a callback that the serializer invokes on a value immediately after all of its members have been populated
/// from the input, letting the value validate or finalize its state.
/// </summary>
/// <remarks>
/// <see cref="OnDeserialized" /> is called once every mapped member has been assigned, so it observes the fully
/// materialized value and is the natural place to enforce cross-member invariants.
/// </remarks>
public interface IOnDeserialized
{
    /// <summary>
    /// Called by the serializer after the value's members have been populated.
    /// </summary>
    void OnDeserialized();
}
