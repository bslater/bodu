// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteFixTests.Serialization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Serialization.Json;

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteFixTests
{
    /// <summary>
    /// Verifies that a row survives a round trip through <see cref="DataContractJsonSerializer" />, the serializer MSTest
    /// uses to give each row of a data-driven test its own test, so that a row that runs past its timeout fails alone
    /// instead of canceling every row after it.
    /// </summary>
    [TestMethod]
    public void Serialization_WhenRowRoundTripsThroughDataContractJson_ShouldEqualTheOriginal()
    {
        ReleaseNoteFix row = Row(caseNumber: "2", rowClass: "dialect", kind: "reject");
        var serializer = new DataContractJsonSerializer(typeof(ReleaseNoteFix));

        using var stream = new MemoryStream();
        serializer.WriteObject(stream, row);
        stream.Position = 0;
        var copy = (ReleaseNoteFix?)serializer.ReadObject(stream);

        Assert.AreEqual(row, copy);
        Assert.AreEqual(row.Name, copy!.Name);
    }
}
