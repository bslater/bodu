// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgNamedPropertyMapTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Verifies the behavior of <see cref="MsgNamedPropertyMap" />, the named-property mapping parser.
/// </summary>
[TestClass]
public partial class MsgNamedPropertyMapTests
{
    /// <summary>The PS_PUBLIC_STRINGS property-set GUID (GUID index 2).</summary>
    internal static readonly Guid PublicStrings = new("00020329-0000-0000-C000-000000000046");

    /// <summary>The PS_MAPI property-set GUID (GUID index 1).</summary>
    internal static readonly Guid PsMapi = new("00020328-0000-0000-C000-000000000046");

    /// <summary>A custom property-set GUID stored in the GUID stream (GUID index 3).</summary>
    internal static readonly Guid CustomSet = new("11111111-2222-3333-4444-555555555555");

    /// <summary>
    /// Opens a built fixture and loads its named-property mapping.
    /// </summary>
    /// <param name="builder">The fixture builder.</param>
    /// <param name="validationLevel">The validation level.</param>
    /// <returns>The loaded mapping.</returns>
    internal static MsgNamedPropertyMap Load(
        MsgFixtureBuilder builder,
        CompoundValidationLevel validationLevel = CompoundValidationLevel.Compatible)
    {
        using MemoryStream stream = builder.Build();
        using var compound = CompoundFile.Open(stream);
        return MsgNamedPropertyMap.Load(compound.RootStorage, validationLevel);
    }

    /// <summary>
    /// Builds a fixture carrying a representative mapping: one numeric name in a custom set and one string name in
    /// PS_PUBLIC_STRINGS.
    /// </summary>
    /// <returns>The fixture builder.</returns>
    internal static MsgFixtureBuilder CreateRepresentative()
    {
        byte[] entries = new byte[16];
        MsgFixtureBuilder.NameIdEntry(0x00008233u, 3, isString: false, 0).CopyTo(entries, 0);
        MsgFixtureBuilder.NameIdEntry(0, 2, isString: true, 1).CopyTo(entries, 8);

        return MsgFixtureBuilder.CreateMinimal()
            .WithNameId(CustomSet.ToByteArray(), entries, MsgFixtureBuilder.NameIdString("Keywords"));
    }
}
