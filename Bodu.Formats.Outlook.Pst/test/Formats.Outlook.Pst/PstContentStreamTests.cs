// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstContentStreamTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Pst;

/// <summary>
/// Verifies the behavior of <see cref="PstContentStream" />, the read-only wrapper that translates a container fault
/// raised by a deferred attachment payload's stream.
/// </summary>
[TestClass]
public partial class PstContentStreamTests
{
    /// <summary>
    /// Creates a content stream over a seekable copy of a payload.
    /// </summary>
    /// <param name="payload">The payload bytes.</param>
    /// <returns>The wrapping stream; the caller disposes it.</returns>
    private static PstContentStream Over(byte[] payload) =>
        new(new MemoryStream(payload, writable: false));
}
