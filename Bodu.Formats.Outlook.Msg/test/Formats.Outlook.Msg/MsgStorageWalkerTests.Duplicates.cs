// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgStorageWalkerTests.Duplicates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgStorageWalkerTests
{
    /// <summary>
    /// Verifies that two storages resolving to the same index — here a second spelling of index zero that the
    /// hexadecimal parser accepts — yield one storage under compatible validation, so a crafted directory cannot
    /// decode the same recipient or attachment twice.
    /// </summary>
    [TestMethod]
    public void EnumerateIndexed_WhenIndexIsDuplicatedUnderCompatible_ShouldReturnOneStoragePerIndex()
    {
        var builder = new MsgFixtureBuilder()
            .AddRecipient(_ => { })
            .AddStorage("__recip_version1.0_# 0000000", new MsgFixtureBuilder(MsgPropertyStreamKind.RecipientOrAttachment));

        List<string> names = EnumerateRecipientNames(builder, declaredCount: 1);

        Assert.AreEqual(1, names.Count, "Duplicate indexes must collapse to one storage.");
    }
}
