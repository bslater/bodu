// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgStorageWalkerTests.EnumerateIndexed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgStorageWalkerTests
{
    /// <summary>
    /// Verifies that recipient storages enumerate in index order regardless of directory order, and that attachment
    /// storages are not mixed in.
    /// </summary>
    [TestMethod]
    public void EnumerateIndexed_WhenStoragesPresent_ShouldReturnIndexOrder()
    {
        var builder = new MsgFixtureBuilder()
            .AddStorage("__recip_version1.0_#00000001", new MsgFixtureBuilder(MsgPropertyStreamKind.RecipientOrAttachment))
            .AddStorage("__recip_version1.0_#00000000", new MsgFixtureBuilder(MsgPropertyStreamKind.RecipientOrAttachment))
            .AddAttachment(_ => { });

        List<string> names = EnumerateRecipientNames(builder, declaredCount: 2);

        CollectionAssert.AreEqual(
            new[] { "__recip_version1.0_#00000000", "__recip_version1.0_#00000001" },
            names);
    }

    /// <summary>
    /// Verifies that a message with no matching storages yields an empty list.
    /// </summary>
    [TestMethod]
    public void EnumerateIndexed_WhenNoStorages_ShouldReturnEmpty()
    {
        Assert.AreEqual(0, EnumerateRecipientNames(MsgFixtureBuilder.CreateMinimal(), declaredCount: 0).Count);
    }

    /// <summary>
    /// Verifies that a storage with an unparsable index suffix is skipped under compatible validation.
    /// </summary>
    [TestMethod]
    public void EnumerateIndexed_WhenSuffixUnparsableUnderCompatible_ShouldSkipStorage()
    {
        var builder = new MsgFixtureBuilder()
            .AddRecipient(_ => { })
            .AddStorage("__recip_version1.0_#NOTAHEXID", new MsgFixtureBuilder(MsgPropertyStreamKind.RecipientOrAttachment));

        List<string> names = EnumerateRecipientNames(builder, declaredCount: 1);

        CollectionAssert.AreEqual(new[] { "__recip_version1.0_#00000000" }, names);
    }
}
