// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvalidEncodedTextKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for malformed encoded text — the input that the decoder is expected to reject —
/// optionally tagged with the variant selector and the expected exception type when the decoder throws rather than
/// returning <see langword="false" />.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Encoded">The malformed encoded text supplied to the decoder.</param>
/// <param name="Variant">An optional variant tag consumed by decoders that expose multiple alphabets.</param>
/// <param name="ExceptionType">
/// The exact exception type the throwing decoder overload must raise, or <see langword="null" /> when the row is only
/// used to assert that a <c>TryDecode</c> overload returns <see langword="false" />.
/// </param>
/// <remarks>
/// <para>
/// Mirrors <see cref="BinaryEncodingKat" /> on the negative-vector side. The two records together cover the happy-path
/// / failure-path split that the plan's binary-test rule requires for decoder tests.
/// </para>
/// </remarks>
public sealed record InvalidEncodedTextKat(
    string Name,
    string Encoded,
    object? Variant = null,
    Type? ExceptionType = null) : IKat;
