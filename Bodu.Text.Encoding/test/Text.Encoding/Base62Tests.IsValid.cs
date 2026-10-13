// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base62Tests.IsValid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base62Tests
{

    /// <summary>
    /// Verifies that <see cref="Base62.IsValid" /> returns <see langword="true" /> exactly when
    /// <see cref="Base62.Decode(string, BaseFormatStyles)" /> succeeds with the same styles, over well-formed,
    /// decorated, and malformed input in every combination of styles.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSweptAcrossStyles_ShouldAgreeWithDecode()
    {
        const string symbols = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_+/";
        var inputs = new HashSet<string>(StringComparer.Ordinal) { new('0', 1300) };
        foreach (var payload in DecodeAgreement.Payloads(8))
            inputs.UnionWith(DecodeAgreement.Mutations(Base62.Encode(payload), symbols));

        var disagreements = new List<string>();
        foreach (var input in inputs)
        {
            foreach (BaseFormatStyles styles in DecodeAgreement.AllStyles)
            {
                var decodes = DecodeAgreement.TryDecode(() => Base62.Decode(input, styles), out _);
                if (Base62.IsValid(input, styles) != decodes)
                    disagreements.Add($"[{styles}] {DecodeAgreement.Quote(input)}: Decode succeeds = {decodes}");
            }
        }

        DecodeAgreement.AssertNone(disagreements);
    }

}
