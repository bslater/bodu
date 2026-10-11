// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantScalarCodecTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides tests for the shared <see cref="InvariantScalarCodec" /> as the delimited serializer compiles it, under the
/// <see cref="InvariantScalarCodec.Policy.Delimited" /> policy: round-trip temporal text, and blank cells that hold no
/// value.
/// </summary>
[TestClass]
public sealed partial class InvariantScalarCodecTests
{
    /// <summary>A UTC instant with sub-second ticks, which only the round-trip form keeps.</summary>
    private static readonly DateTime s_instant =
        new DateTime(2026, 8, 20, 11, 22, 33, DateTimeKind.Utc).AddTicks(1234567);
}
