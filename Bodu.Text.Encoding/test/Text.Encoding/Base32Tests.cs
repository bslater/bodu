// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Base32" />. Partial files split coverage by member or subject contract per the
/// repository test convention.
/// </summary>
[TestClass]
public sealed partial class Base32Tests
{

    /// <summary>
    /// Returns the ASCII bytes for the supplied string, used to build RFC 4648 reference vectors.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <returns>The ASCII byte representation.</returns>
    private static byte[] Ascii(string value) => System.Text.Encoding.ASCII.GetBytes(value);

    /// <summary>
    /// Returns the inputs the validation sweeps hold to <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" />:
    /// padded and unpadded encodings of every payload up to eleven bytes (every terminal-quantum length), each with
    /// the mutations of <see cref="DecodeAgreement.Mutations" />, plus hand-picked partially padded, over-padded, and
    /// misplaced-padding inputs.
    /// </summary>
    /// <param name="variant">The variant whose encodings seed the inputs.</param>
    /// <returns>The distinct inputs.</returns>
    private static HashSet<string> GetValidationSweepInputs(Base32Variant variant)
    {
        const string symbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567" + "0189" + "abcdefghijklmnopqrstuvwxyz";
        var inputs = new HashSet<string>(StringComparer.Ordinal)
        {
            "MY======", "MY=", "MY", "MZ======", "MY========", "MZXW6YTBOI=", "MZ=XW6YT", "D1G2=", "ybnd=", "========",
        };

        foreach (var payload in DecodeAgreement.Payloads(11))
        {
            foreach (BaseFormattingOptions options in new[] { BaseFormattingOptions.None, BaseFormattingOptions.OmitPadding })
                inputs.UnionWith(DecodeAgreement.Mutations(Base32.Encode(payload, variant, options), symbols));
        }

        return inputs;
    }

}
