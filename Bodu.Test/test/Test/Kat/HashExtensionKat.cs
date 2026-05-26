// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashExtensionKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for the hashing extension surface (synchronous and asynchronous
/// append, compute, verify, try-verify methods exposed by <c>Bodu.IO.Hashing.Extensions</c>).
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The raw byte payload.</param>
/// <param name="ExpectedHash">The expected digest bytes.</param>
/// <param name="BufferSize">The async/streaming buffer size to use when exercising the streamed extensions. Defaults to the .NET <c>Stream.CopyToAsync</c> default of 81920 bytes.</param>
public sealed record HashExtensionKat(
    string Name,
    byte[] Input,
    byte[] ExpectedHash,
    int BufferSize = 81920) : IKat;
