// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Tests.ReferenceKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public partial class Poly1305Tests
{
    // ── RFC 8439 Appendix A.3 Poly1305 MAC reference vectors ──────────────────────────────────
    //
    // Loaded dynamically from the embedded RFC 8439 source text (Appendix A.3, "Poly1305 Message Authentication
    // Code"): eleven per-vector one-time-key / message / tag triples, including the edge cases that exercise partial
    // reduction, modular overflow, and the 5*H+L carry paths.

    /// <summary>Resource name of the embedded RFC 8439 source text.</summary>
    private const string Poly1305Rfc8439ResourceName = "Bodu.Security.Cryptography.Rfc8439.txt";

    /// <summary>
    /// Loads the RFC 8439 Appendix A.3 Poly1305 vectors from the embedded RFC text and yields them as
    /// <see cref="DynamicDataAttribute" /> rows, mapping the one-time key, message, and tag onto a
    /// <see cref="MessageDigestKnownAnswer" />.
    /// </summary>
    /// <returns>One row per Appendix A.3 vector.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> Poly1305Rfc8439KatData()
    {
        using Stream stream = typeof(Poly1305Tests).Assembly.GetManifestResourceStream(Poly1305Rfc8439ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Poly1305Rfc8439ResourceName}' is missing.");

        foreach (Rfc8439TestVector vector in Rfc8439VectorReader.Read(stream, "A.3.  Poly1305 Message Authentication Code"))
        {
            // Vectors #1-#4 give a combined 32-byte "One-time Poly1305 Key" and "Text to MAC"; the edge-case vectors
            // #5-#11 split the key into R and S and name the message "data". The "Tag"/"tag" label varies only in case.
            byte[] key = vector.Fields.TryGetValue("One-time Poly1305 Key", out byte[]? combined)
                ? combined
                : [.. vector.Field("R"), .. vector.Field("S")];
            byte[] message = vector.Fields.TryGetValue("Text to MAC", out byte[]? text) ? text : vector.Field("data");

            yield return new object[]
            {
                new MessageDigestKnownAnswer
                {
                    Name = $"RFC 8439 Appendix A.3 Poly1305 #{vector.Number}",
                    Key = key,
                    Message = message,
                    Digest = vector.Field("Tag"),
                },
            };
        }
    }

    /// <summary>
    /// Verifies that <see cref="Poly1305" /> produces the published tag for every RFC 8439 Appendix A.3 vector under
    /// that vector's one-time key.
    /// </summary>
    /// <param name="vector">The Poly1305 reference vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Poly1305Rfc8439KatData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ComputeHash_WithRfc8439AppendixA3Vector_ShouldMatchTag(MessageDigestKnownAnswer vector)
    {
        using var poly = new Poly1305 { Key = vector.Key! };

        byte[] actual = poly.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual, $"Poly1305 tag mismatch for {vector.Name}.");
    }
}
