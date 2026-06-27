// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85GitTests.Registration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85GitTests
{
    /// <summary>
    /// Verifies that <see cref="BinaryEncodings.Get(string)" /> resolves every Git alias to the Git compact encoding.
    /// </summary>
    /// <param name="name">The alias to resolve.</param>
    [TestMethod]
    [DataRow("base85-git")]
    [DataRow("git-base85")]
    [DataRow("b85")]
    [DataRow("BASE85-GIT")]
    public void Get_WhenGitAlias_ShouldResolveToBase85Git(string name)
    {
        IBinaryEncoding encoding = BinaryEncodings.Get(name);

        Assert.AreSame(BinaryEncodings.Base85Git, encoding);
    }

    /// <summary>
    /// Verifies that the Git registration round-trips arbitrary bytes through the <see cref="IBinaryEncoding" />
    /// contract.
    /// </summary>
    [TestMethod]
    public void Base85Git_AsBinaryEncoding_ShouldRoundTrip()
    {
        IBinaryEncoding encoding = BinaryEncodings.Base85Git;
        byte[] original = Ascii("hello world");

        string encoded = encoding.Encode(original);
        byte[] decoded = encoding.Decode(encoded);

        Assert.AreEqual("Xk~0{Zy<MXa%^M", encoded);
        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that adding the Git variant leaves the existing Base85 aliases pointing at their original encodings.
    /// </summary>
    [TestMethod]
    public void Get_WhenExistingBase85Aliases_ShouldRemainUnchanged()
    {
        Assert.AreSame(BinaryEncodings.Ascii85, BinaryEncodings.Get("base85"));
        Assert.AreSame(BinaryEncodings.Ascii85, BinaryEncodings.Get("ascii85"));
        Assert.AreSame(BinaryEncodings.Z85, BinaryEncodings.Get("z85"));
    }
}
