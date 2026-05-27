// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Marks a known-answer test (KAT) row that carries a human-readable <see cref="Name" /> for failure diagnostics.
/// </summary>
/// <remarks>
/// <para>
/// The interface exists so that <see cref="KatDisplayName.GetDisplayName" /> can produce scenario-named test rows for
/// any KAT shape without resorting to reflection over individual record types. Every concrete KAT record in
/// <c>Bodu.Test.Kat</c> implements this interface and exposes a positional <c>Name</c> parameter.
/// </para>
/// </remarks>
public interface IKat
{
    /// <summary>
    /// Gets the short, human-readable label that identifies this row in test output.
    /// </summary>
    /// <returns>A non-empty descriptive label, for example <c>"int zero"</c> or <c>"RFC4648 empty"</c>.</returns>
    string Name { get; }
}
