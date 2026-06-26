// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KatProvenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Describes where a cryptographic known-answer test vector came from — the kind of source and a human-readable
/// citation — so failure diagnostics make the authority and origin of a vector obvious.
/// </summary>
/// <param name="Kind">The category of source the vector was taken from.</param>
/// <param name="Citation">
/// A concise human-readable citation, for example <c>"FIPS-197 Appendix C.1"</c>, <c>"RFC 8439 Section 2.4.2"</c>, or
/// <c>"NIST ACVP ML-KEM-keyGen vsId 42"</c>.
/// </param>
/// <param name="Notes">Optional free-form clarification, such as a caveat about how the vector was derived.</param>
public sealed record KatProvenance(KatSourceKind Kind, string Citation, string? Notes = null)
{
    /// <summary>
    /// Creates a provenance record for a vector taken from a formal standard (FIPS, NIST SP, ISO).
    /// </summary>
    /// <param name="citation">The standard reference, for example <c>"FIPS-197 Appendix C.1"</c>.</param>
    /// <returns>A <see cref="KatProvenance" /> with <see cref="Kind" /> set to <see cref="KatSourceKind.Standard" />.</returns>
    public static KatProvenance Standard(string citation) =>
        new(KatSourceKind.Standard, citation);

    /// <summary>
    /// Creates a provenance record for a vector published in an RFC.
    /// </summary>
    /// <param name="citation">The RFC reference, for example <c>"RFC 8439 Section 2.4.2"</c>.</param>
    /// <returns>A <see cref="KatProvenance" /> with <see cref="Kind" /> set to <see cref="KatSourceKind.Rfc" />.</returns>
    public static KatProvenance Rfc(string citation) =>
        new(KatSourceKind.Rfc, citation);

    /// <summary>
    /// Creates a provenance record for a vector published in an Internet-Draft.
    /// </summary>
    /// <param name="citation">The draft reference, for example <c>"draft-irtf-cfrg-xchacha-03 Appendix A.3.1"</c>.</param>
    /// <returns>A <see cref="KatProvenance" /> with <see cref="Kind" /> set to <see cref="KatSourceKind.InternetDraft" />.</returns>
    public static KatProvenance InternetDraft(string citation) =>
        new(KatSourceKind.InternetDraft, citation);

    /// <summary>
    /// Creates a provenance record for a vector taken from a reference implementation or external test suite.
    /// </summary>
    /// <param name="citation">The implementation or suite reference.</param>
    /// <returns>A <see cref="KatProvenance" /> with <see cref="Kind" /> set to <see cref="KatSourceKind.ReferenceImplementation" />.</returns>
    public static KatProvenance ReferenceImplementation(string citation) =>
        new(KatSourceKind.ReferenceImplementation, citation);

    /// <summary>
    /// Creates a provenance record for a vector composed from independently-tested primitives or an independent
    /// implementation rather than taken from a single external published source.
    /// </summary>
    /// <param name="citation">A description of how the vector was derived or cross-validated.</param>
    /// <returns>A <see cref="KatProvenance" /> with <see cref="Kind" /> set to <see cref="KatSourceKind.DerivedOracle" />.</returns>
    public static KatProvenance DerivedOracle(string citation) =>
        new(KatSourceKind.DerivedOracle, citation);

    /// <summary>
    /// Creates a provenance record for an in-tree regression baseline captured from this library's own output.
    /// </summary>
    /// <param name="citation">A short description of the baseline and any tracking issue.</param>
    /// <returns>A <see cref="KatProvenance" /> with <see cref="Kind" /> set to <see cref="KatSourceKind.InternalRegression" />.</returns>
    public static KatProvenance InternalRegression(string citation) =>
        new(KatSourceKind.InternalRegression, citation);

    /// <inheritdoc />
    public override string ToString() =>
        Notes is null ? $"{Kind}: {Citation}" : $"{Kind}: {Citation} ({Notes})";
}
