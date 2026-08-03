// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgStorageWalkerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Verifies the behavior of <see cref="MsgStorageWalker" />, the indexed child-storage enumerator.
/// </summary>
[TestClass]
public partial class MsgStorageWalkerTests
{
    /// <summary>
    /// Opens a built fixture and enumerates its recipient storages.
    /// </summary>
    /// <param name="builder">The fixture builder.</param>
    /// <param name="declaredCount">The declared recipient count to validate against.</param>
    /// <param name="validationLevel">The validation level.</param>
    /// <returns>The names of the enumerated storages, in walker order.</returns>
    internal static List<string> EnumerateRecipientNames(
        MsgFixtureBuilder builder,
        uint declaredCount,
        CompoundValidationLevel validationLevel = CompoundValidationLevel.Compatible)
    {
        using MemoryStream stream = builder.Build();
        using var compound = CompoundFile.Open(stream);
        List<CompoundStorage> storages = MsgStorageWalker.EnumerateIndexed(
            compound.RootStorage, MsgStreamNames.RecipientStoragePrefix, declaredCount, validationLevel);

        var names = new List<string>(storages.Count);
        foreach (CompoundStorage storage in storages)
            names.Add(storage.Name);

        return names;
    }
}
