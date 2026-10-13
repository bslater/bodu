// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Base64" />. Partial files split coverage by member or subject contract per the
/// repository test convention.
/// </summary>
[TestClass]
public sealed partial class Base64Tests
{

    /// <summary>
    /// Returns the ASCII bytes for <paramref name="value" />, used to build reference vectors.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <returns>The ASCII byte representation.</returns>
    private static byte[] Ascii(string value) => System.Text.Encoding.ASCII.GetBytes(value);

    /// <summary>
    /// Indicates whether <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> throws
    /// <see cref="OverflowException" /> rather than <see cref="FormatException" /> for <paramref name="input" />: a
    /// strictly padded decode of padded input shorter than one four-character quantum (for example <c>QQ=</c>). That
    /// is a separate defect from the validation sweeps' concern, so they leave this shape out.
    /// </summary>
    /// <param name="input">The candidate input.</param>
    /// <param name="variant">The variant the sweep decodes with.</param>
    /// <param name="styles">The styles the sweep decodes with.</param>
    /// <returns><see langword="true" /> when the decode would throw <see cref="OverflowException" />.</returns>
    private static bool IsShortPaddedStrictInput(string input, Base64Variant variant, BaseFormatStyles styles)
    {
        if (variant == Base64Variant.UrlSafe || styles.HasFlag(BaseFormatStyles.AllowMissingPadding))
            return false;

        var retained = variant == Base64Variant.Mime || styles.HasFlag(BaseFormatStyles.IgnoreWhitespace)
            ? string.Concat(input.Where(c => c is not (' ' or '\t' or '\r' or '\n')))
            : input;

        return retained.Length is > 0 and < 4 && retained[^1] == '=';
    }

    /// <summary>
    /// Returns the inputs the validation sweeps hold to <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" />:
    /// padded and unpadded encodings of every payload up to ten bytes, each with the mutations of
    /// <see cref="DecodeAgreement.Mutations" />, plus hand-picked non-canonical, partially padded, over-padded, and
    /// wrapped inputs.
    /// </summary>
    /// <param name="variant">The variant whose encodings seed the inputs.</param>
    /// <returns>The distinct inputs.</returns>
    private static HashSet<string> GetValidationSweepInputs(Base64Variant variant)
    {
        const string symbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/-_";
        var inputs = new HashSet<string>(StringComparer.Ordinal)
        {
            "QR==", "QQ==", "QQ=", "QQ", "QUI=", "QUJ=", "QUI", "QUJ", "TQ===", "Zm9vYmE==", "Zm9vYmE", "Zm9v\r\nYmFy",
            "Zm9vYmFy====", "QQ==QQ==", "====",
        };

        foreach (var payload in DecodeAgreement.Payloads(10))
        {
            foreach (BaseFormattingOptions options in new[] { BaseFormattingOptions.None, BaseFormattingOptions.OmitPadding })
                inputs.UnionWith(DecodeAgreement.Mutations(Base64.Encode(payload, variant, options), symbols));
        }

        return inputs;
    }

}
