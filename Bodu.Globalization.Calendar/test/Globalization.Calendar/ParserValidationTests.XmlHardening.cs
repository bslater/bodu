// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserValidationTests.XmlHardening.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class ParserValidationTests
{
    /// <summary>
    /// Verifies that an untrusted document declaring a DTD is rejected with <see cref="FormatException" />, locking the
    /// explicit <c>DtdProcessing.Prohibit</c> reader setting that blocks XXE and entity-expansion (billion-laughs)
    /// attacks so a future change cannot silently re-enable DTD processing.
    /// </summary>
    [TestMethod]
    public void Load_WhenXmlDeclaresDtd_ShouldThrowFormatException()
    {
        const string Xml =
            "<!DOCTYPE NotableDateResource [<!ENTITY x \"expanded\">]>" +
            "<NotableDateResource xmlns=\"urn:bodu:globalization:calendar\" />";

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateResourceLoader.Load(Xml);
        });
    }

    /// <summary>
    /// Verifies that an untrusted document embedding an external-entity reference is rejected with
    /// <see cref="FormatException" /> rather than resolving it, confirming the resolver-free, DTD-prohibited parse.
    /// </summary>
    [TestMethod]
    public void Load_WhenXmlReferencesExternalEntity_ShouldThrowFormatException()
    {
        const string Xml =
            "<!DOCTYPE NotableDateResource [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]>" +
            "<NotableDateResource xmlns=\"urn:bodu:globalization:calendar\"><Name>&xxe;</Name></NotableDateResource>";

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateResourceLoader.Load(Xml);
        });
    }
}
