// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MalformedRecordKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Formats.Excel;

/// <summary>
/// A known-answer test row for malformed worksheet records: a sequence of framed BIFF8 records the reader is expected
/// to reject with an <see cref="ExcelBinaryFormatException" />.
/// </summary>
/// <param name="Name">The human-readable scenario label surfaced in test output.</param>
/// <param name="Records">The framed BIFF8 records that form the malformed worksheet substream body.</param>
/// <remarks>
/// Every row in this catalogue rejects with the same exact exception type, so the consuming test asserts
/// <see cref="ExcelBinaryFormatException" /> directly rather than carrying a per-row exception type.
/// </remarks>
public sealed record MalformedRecordKat(string Name, byte[][] Records)
    : IKat;
