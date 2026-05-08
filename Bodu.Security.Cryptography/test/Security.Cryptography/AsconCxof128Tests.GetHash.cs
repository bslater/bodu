// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128Tests.GetHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconCxof128Tests
{
    /// <summary>
    /// Verifies that the output of <see cref="AsconCxof128" /> with an empty customisation string
    /// differs from the output of <see cref="AsconXof128" /> for the same message. The domain-
    /// separation constant ensures the two algorithms always produce distinct outputs.
    /// </summary>
    [TestMethod]
    public void GetHash_WithEmptyCustomization_ShouldDifferFromXof128Output()
    {
        byte[] message = [0x01, 0x02, 0x03, 0x04];

        using AsconCxof128 cxof = new AsconCxof128();
        cxof.Customize(ReadOnlySpan<byte>.Empty);
        cxof.Absorb(message);
        byte[] cxofOutput = cxof.GetHash(32);

        byte[] xofOutput = AsconXof128.HashData(message, 32);

        CollectionAssert.AreNotEqual(cxofOutput, xofOutput,
            "ASCON-CXOF128 with empty customization must produce different output than ASCON-XOF128.");
    }

    /// <summary>
    /// Verifies that different customisation strings produce different outputs for the same message.
    /// </summary>
    [TestMethod]
    public void GetHash_WithDifferentCustomizations_ShouldProduceDifferentOutputs()
    {
        byte[] message = [0xDE, 0xAD];

        using AsconCxof128 cxof1 = new AsconCxof128();
        cxof1.Customize([0x01]);
        cxof1.Absorb(message);
        byte[] output1 = cxof1.GetHash(32);

        using AsconCxof128 cxof2 = new AsconCxof128();
        cxof2.Customize([0x02]);
        cxof2.Absorb(message);
        byte[] output2 = cxof2.GetHash(32);

        CollectionAssert.AreNotEqual(output1, output2,
            "Different customisation strings must produce different outputs for the same message.");
    }

    /// <summary>
    /// Verifies that the same customisation string and message always produce the same output
    /// (determinism).
    /// </summary>
    [TestMethod]
    public void GetHash_SameCustomizationAndMessage_ShouldProduceIdenticalOutput()
    {
        byte[] customization = [0x41, 0x42, 0x43]; // "ABC"
        byte[] message = [0x10, 0x20, 0x30];

        using AsconCxof128 first = new AsconCxof128();
        first.Customize(customization);
        first.Absorb(message);
        byte[] output1 = first.GetHash(32);

        using AsconCxof128 second = new AsconCxof128();
        second.Customize(customization);
        second.Absorb(message);
        byte[] output2 = second.GetHash(32);

        CollectionAssert.AreEqual(output1, output2,
            "ASCON-CXOF128 must be deterministic for the same customisation and message.");
    }

    /// <summary>
    /// Verifies that customising then absorbing the same bytes in the opposite order produces
    /// different output, confirming the domain separation is order-sensitive.
    /// </summary>
    [TestMethod]
    public void GetHash_CustomizeAndAbsorbSwapped_ShouldProduceDifferentOutputs()
    {
        byte[] bytes = [0x11, 0x22, 0x33];

        using AsconCxof128 normal = new AsconCxof128();
        normal.Customize(bytes);
        normal.Absorb([0x44]);
        byte[] outputNormal = normal.GetHash(32);

        using AsconCxof128 swapped = new AsconCxof128();
        swapped.Customize([0x44]);
        swapped.Absorb(bytes);
        byte[] outputSwapped = swapped.GetHash(32);

        CollectionAssert.AreNotEqual(outputNormal, outputSwapped,
            "Swapping the customisation and message bytes must produce different output.");
    }

    // ── Without Customize ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that skipping <see cref="AsconCxof128.Customize" /> and directly calling
    /// <see cref="AsconXof{T}.Absorb" /> succeeds and produces output that differs from the
    /// explicitly customised case.
    /// </summary>
    [TestMethod]
    public void GetHash_WithoutCustomize_ShouldSucceedAndDifferFromCustomizedOutput()
    {
        byte[] message = [0x01, 0x02, 0x03];

        using AsconCxof128 uncustomized = new AsconCxof128();
        uncustomized.Absorb(message);
        byte[] outputUncustomized = uncustomized.GetHash(32);

        using AsconCxof128 customized = new AsconCxof128();
        customized.Customize([0x01]);
        customized.Absorb(message);
        byte[] outputCustomized = customized.GetHash(32);

        Assert.IsNotNull(outputUncustomized);
        Assert.AreEqual(32, outputUncustomized.Length);
        CollectionAssert.AreNotEqual(outputUncustomized, outputCustomized,
            "Un-customised and customised instances must produce different outputs.");
    }
}
