// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WycheproofAeadKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Reads the curated Project Wycheproof AEAD fixtures - <c>Name</c>, <c>Key</c>, <c>Iv</c>, <c>Aad</c>, <c>Msg</c>,
/// <c>Ct</c>, <c>Tag</c>, and <c>Result</c> fields in the <see cref="HexFieldKatReader" /> block format - as
/// <see cref="AeadKnownAnswer" /> rows.
/// </summary>
public static class WycheproofAeadKatReader
{
    /// <summary>
    /// Reads the rows of an embedded fixture whose result is <c>valid</c>, or those whose result is <c>invalid</c>.
    /// </summary>
    /// <param name="anchor">A type in the assembly that embeds the fixture.</param>
    /// <param name="resourceName">The fixture's logical resource name.</param>
    /// <param name="sourceFile">The Wycheproof file the fixture was curated from, for the rows' provenance.</param>
    /// <param name="valid"><see langword="true" /> to read the valid rows; <see langword="false" /> for the invalid ones.</param>
    /// <returns>One row per vector, with the tag detached from the ciphertext.</returns>
    /// <exception cref="InvalidOperationException">The fixture is not embedded in the assembly.</exception>
    public static IEnumerable<AeadKnownAnswer> Read(Type anchor, string resourceName, string sourceFile, bool valid)
    {
        using Stream stream = anchor.Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        string wanted = valid ? "valid" : "invalid";
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            if (HexFieldKatReader.GetRequired(record, "Result") != wanted)
                continue;

            yield return new AeadKnownAnswer
            {
                Name = "Wycheproof " + HexFieldKatReader.GetRequired(record, "Name"),
                Provenance = KatProvenance.ReferenceImplementation("Project Wycheproof " + sourceFile),
                Key = Field(record, "Key"),
                Nonce = Field(record, "Iv"),
                AssociatedData = Field(record, "Aad"),
                Plaintext = Field(record, "Msg"),
                Ciphertext = Field(record, "Ct"),
                Tag = Field(record, "Tag"),
                Layout = AeadKatOutputLayout.CiphertextThenTag,
            };
        }
    }

    /// <summary>
    /// Decodes a hex field that may be empty.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="name">The field's label.</param>
    /// <returns>The decoded bytes; empty when the field is.</returns>
    private static byte[] Field(Dictionary<string, string> record, string name)
    {
        string value = HexFieldKatReader.GetRequired(record, name);
        return value.Length == 0 ? [] : Hex(value);
    }
}
