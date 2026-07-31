// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgDecodeKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Represents a known-answer row for fixed-length property decoding: a raw tag and inline value paired with the
/// expected CLR value.
/// </summary>
/// <remarks>
/// A bespoke record (rather than the shared generics) because a row carries two coordinated inputs — the tag selecting
/// the decode path and the 8-byte inline value — plus an <see cref="object" />-typed expectation compared via
/// <see cref="object.Equals(object)" />.
/// </remarks>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Tag">The raw 32-bit property tag.</param>
/// <param name="Raw">The 8-byte inline value, little-endian.</param>
/// <param name="Expected">The expected decoded CLR value.</param>
public sealed record MsgDecodeKat(
    string Name,
    uint Tag,
    ulong Raw,
    object? Expected) : IKat;
