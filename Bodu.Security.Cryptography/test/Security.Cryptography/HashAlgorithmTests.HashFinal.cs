// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.HashFinal.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that the implementation-specific "unsupported hash size" diagnostic message
    /// used by <see cref="HashAlgorithm.HashFinal" /> (when it throws for an out-of-range
    /// <see cref="HashAlgorithm.HashSize" /> configuration) contains no rename-refactor
    /// artefacts such as stray <c>this.</c> tokens. The throw path is generally unreachable
    /// from the public API, so this is a literal-level guard implemented via an overridable
    /// expected-fragment property.
    /// </summary>
    [TestMethod]
    public void HashFinal_UnsupportedHashSizeMessage_ShouldNotContainRefactorArtefacts()
    {
        string? fragment = UnsupportedHashSizeMessageFragment;
        if (fragment is null)
        {
            Assert.Inconclusive(
                $"{typeof(TAlgorithm).Name} does not expose an 'unsupported hash size' message fragment to audit.");
            return;
        }

        Assert.IsFalse(fragment.Contains("this."), $"Expected message fragment must not contain 'this.'. Actual: '{fragment}'");
        Assert.IsFalse(fragment.Contains(" t "), $"Expected message fragment must not contain stray ' t '. Actual: '{fragment}'");
        Assert.IsTrue(fragment.Length > 0, "Expected message fragment must not be empty.");
    }

    /// <summary>
    /// Gets the literal message fragment used by this algorithm's <c>HashFinal</c> path when
    /// it throws for an unsupported hash size. Concrete test classes whose algorithm does not
    /// have such a path should return <see langword="null" /> (the default), in which case the
    /// audit test is marked inconclusive.
    /// </summary>
    protected virtual string? UnsupportedHashSizeMessageFragment => null;
}
